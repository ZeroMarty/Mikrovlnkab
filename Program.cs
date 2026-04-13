using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automation.BDaq;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Mikrovlnkab
{
    internal class Program
    {
        static DeviceInformation zarizeni = new DeviceInformation();
        static InstantDoCtrl vystup = new InstantDoCtrl();
        static InstantDiCtrl vstup = new InstantDiCtrl();
        static byte[] segmenty = new byte[]
        {
             // segment vpravo
            0b01111111,
            0b10111111,
            0b11011111,
            0b11101111//segment vlevo
        };
        static byte segmentoff = 0b11110000;
        static byte segmentnull = 0b11100000; //nula
        static byte[] cisla = new byte[] //Adresy EPROM
        {
            0b11100000,
            0b11100001,
            0b11100010,
            0b11100011,
            0b11100100,
            0b11100101,
            0b11100110,
            0b11100111,
            0b11101000,
            0b11101001
        };
        static string cesta = "kod.txt";
        static byte zamekdown = 0b11110111;
        static byte zamekup = 0b00001000;
        static byte zvukdown = 0b11111011;
        static byte zvukup = 0b00000100;
        static byte motordown = 0b11111101;
        static byte motorup = 0b00000010;
        static byte svetlodown = 0b11111110;
        static byte svetloup = 0b00000001;
        static byte zapis = 0xff; //všeobecné výstupy
        static byte zapis2 = 0xff; //Čísla na displeji
        static int kod = 0;
        static List<int> zamek = new List<int>(); //list zámku
        static List<int> klic = new List<int>(); // list klíče zadaného uživatelem 
        static bool jekod = false;
        static Stopwatch dvere = new Stopwatch();
        static Stopwatch sw = new Stopwatch();

        static void Main(string[] args) 
        {
            setup();
            while (true)
            {
                    if (jekod == false)// zadání kódu
                    {
                        vstup.Read(0, out byte dveretrezoru); //Kontrola dveří
                        if ((dveretrezoru & 1 << 0) == 0)
                        {
                            Console.WriteLine("Otevřete dveře\n");
                            while((dveretrezoru & 1 << 0) == 0)
                            {
                                vstup.Read(0, out dveretrezoru);
                                unlock();
                            }
                        }
                        else
                        {
                        svetlo100();
                        Console.WriteLine("Zapsání kódu \n");
                        Console.WriteLine("5 sekund prodleva po každé číslici, je zapotřebí u každé části kódu psát jiným tlačítkem\n");
                        for (int i = 0; i < 4; i++)
                        {
                            byte operace = zapnutisegmentu(i); //Metoda pro výběr operace z pole

                            zapis = (byte)(zapis | segmentoff); //Nejprve dá vsechny segmenty do 1 pak pomocí vybrané operace vynuluje ten potřebný
                            zapis = (byte)(zapis & operace);
                            vystup.Write(0, zapis);
                            Thread.Sleep(20);
                            byte display = nastavenisegmentu(kod); //funguje podobně co zapínání segmentu
                            zapis2 = (byte)(zapis2 & segmentnull);
                            zapis2 = (byte)(zapis2 | display);
                            vystup.Write(1, zapis2);
                            Console.WriteLine("Měřim čas");
                            while (sw.Elapsed.Seconds < 5) //5 sekund na segment
                            {
                                kontrola();
                                vstup.Read(0, out byte data);
                                if ((data & 1 << 1) == 0)
                                {
                                    Console.WriteLine($"Píšu kód" + kod);
                                    kod++; //Po stisku tkačítka přičte 1
                                    if (kod > 9)
                                    {
                                        kod = 0; //při přetečení vynuluje
                                    }
                                    display = nastavenisegmentu(kod); //zobrazení aktuálního kódu
                                    zapis2 = (byte)(zapis2 & segmentnull);
                                    zapis2 = (byte)(zapis2 | display);
                                    vystup.Write(1, zapis2);
                                    Thread.Sleep(220); //díky prodlevě se zmírní citlivost tlačítek
                                    sw.Restart(); //reset stopek při zadání
                                }
                            }
                            sw.Reset();
                            zamek.Add(kod); //Přidávání částí kódu 1 po drzg
                            kod = 0;
                        }
                        vstup.Read(0, out byte trezor);
                        while ((trezor & 1 << 0) != 0) //Čekáni na zavření dveří
                        {
                            dvere.Reset();//
                            dvere.Start();
                            Console.WriteLine("\nČekání na zavření dveří\n");
                            while ((trezor & 1 << 0) != 0)
                            {
                                vstup.Read(0, out trezor);
                                if (dvere.Elapsed.Seconds > 60) //alarm pokud je otevřeno déle jak 60 sekund od 1. hlášení
                                {
                                    dvere.Restart();
                                    zvuk();
                                }
                            }

                        }
                        using (StreamWriter writer = new StreamWriter(cesta)) //výpis kódu do souboru
                        {
                            foreach (int i in zamek)
                            {
                                writer.Write(i);
                            }
                        }
                        jekod = true;
                        zamek.Clear();
                    }
                    svetlo0();
                    }
                    else if (jekod == true) //Hádání kódu
                    {
                        int pocitadlo = 0; //počítání pokusů
                        using (StreamReader reader = new StreamReader(cesta))//čtení kódu
                        {
                            string kod = reader.ReadToEnd();
                            foreach (char znak in kod)
                            {
                                int cislo = (int)znak;
                                zamek.Add(cislo - 48); //nečte čísla ale znaky, je zapotřebí odečíst '0'
                            }
                        }
                        while (jekod == true) //mód hádání
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Console.WriteLine("Hádání kódu \n");
                                Console.WriteLine("5 sekund prodleva pro každou číslici, je zapotřebí u každé části kódu psát jiným tlačítkem\n");
                                byte operace = zapnutisegmentu(i);
                                zapis = (byte)(zapis | segmentoff & svetlodown);
                                zapis = (byte)(zapis & operace);
                                vystup.Write(0, zapis);
                                Thread.Sleep(20);
                                byte display = nastavenisegmentu(kod);
                                zapis2 = (byte)(zapis2 & segmentnull);
                                zapis2 = (byte)(zapis2 | display);
                                vystup.Write(1, zapis2);
                                sw.Start();
                                Console.WriteLine("Měřim čas\n");
                                beh(); //točení a světlo
                                while (sw.Elapsed.Seconds < 5) //funguje stejně co hádání
                                {
                                    beh();
                                    zapis = (byte)(zapis & motordown);
                                    Thread.Sleep(5);
                                    vystup.Write(zapis, 0);
                                    zapis = (byte)(zapis | motorup);
                                    Thread.Sleep(5);
                                    vystup.Write(zapis, 0);
                                    vstup.Read(0, out byte data);
                                    if ((data & 1 << 1) == 0)
                                    {
                                        Console.WriteLine($"Píšu kód:" + kod);
                                        kod++;
                                        if (kod > 9)
                                        {
                                            kod = 0;
                                        }
                                        display = nastavenisegmentu(kod);
                                        zapis2 = (byte)(zapis2 & segmentnull);
                                        zapis2 = (byte)(zapis2 | display);
                                        vystup.Write(1, zapis2);
                                        Thread.Sleep(220);
                                        sw.Restart();
                                    }

                                }
                                sw.Reset();
                                klic.Add(kod);
                                kod = 0;
                            }
                            bool otevrise = odemknuti(klic, zamek); //kontrola klíče oproti zámku
                            if (otevrise == true)//pokud správně, otevře se, smaže kód a přepne se do režimu nastavení
                            {
                                Console.WriteLine("\nSprávně\n");
                                unlock();
                                klic.Clear();
                                zamek.Clear();
                                jekod = false;
                                File.Delete(cesta);
                                kod = 0;
                                svetlo100();
                            }
                            else if (otevrise == false) //pokud špatně, připočítá na počítadle
                            {
                                Console.WriteLine("\nŠpatně\n");
                                pocitadlo++;
                                klic.Clear();
                                kod = 0;
                            }

                            if (pocitadlo == 3) //pokud je toto 3. neúspěšný pokus, pak alarm
                            {
                                Console.WriteLine("\nŠpatně 3x\n");
                                zvuk();
                                pocitadlo = 0;
                            }
                        }

                    
                }


            }
        }
        static void unlock()
        {
            zapis = (byte)(zapis & zamekdown);
            vystup.Write(0, zapis);
            Thread.Sleep(200);
            zapis = (byte)(zapis | zamekup);
            vystup.Write(0, zapis);
            Thread.Sleep(200);
        }

        static void kontrola()
        {
            vstup.Read(0, out byte dveretrezoru);
            if ((dveretrezoru & 1 << 0) == 0)
            {
                Console.WriteLine("Otevřete dveře\n");
                while ((dveretrezoru & 1 << 0) == 0)
                {
                    sw.Restart();
                    vstup.Read(0, out dveretrezoru);
                    unlock();
                }
            }
        }
        static void motor()
        {
            zapis = (byte)(zapis & motordown);
            vystup.Write(0, zapis);
            Thread.Sleep(5);
            zapis = (byte)(zapis | motorup);
            vystup.Write(0, zapis);
            Thread.Sleep(5);
        }
        static void setup()
        {
            zarizeni.Description = "PCIE-1730,BID#0";
            zarizeni.DeviceMode = AccessMode.ModeWrite;
            vystup.SelectedDevice = zarizeni;
            vstup.SelectedDevice = zarizeni;
            //zapis = (byte)(zapis & segment1 &);
            vystup.Write(0, zapis);
            vystup.Write(1, zapis);
            
            if(File.Exists(cesta))
            {
                jekod = true;
            }
            else
            {
                jekod = false;
            }
            Console.WriteLine($"Existuje kód: {jekod}\n");
        }
        static void beh()
        {
            motor();
            svetlo50();
        }
        
        
        
        static byte nastavenisegmentu(int cislo)
        {
            byte operace = cisla[cislo];
            return operace;
        }

        static bool odemknuti(List<int> a, List<int> b)
        {
            int spravne = 0; //počítadlo správných cifer
            for(int i = 0;i<4;i++)
            {
                if (a[i] == b[i]) //srovnává části kódu v obou listech
                {
                    spravne++; 
                }
            }
            if(spravne == 4)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static byte zapnutisegmentu(int cislo)
        {
            byte operace = segmenty[cislo];
            return operace;
        }

        static void zvuk()
        {
            for (int i = 0; i < 500; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.ElapsedMilliseconds < 1)
                {

                }
                zapis = (byte)(zapis & zvukdown);
                vystup.Write(0, zapis);
                sw.Reset();
                sw.Start();
                while (sw.ElapsedMilliseconds < 1)
                {

                }
                zapis = (byte)(zapis | zvukup);
                vystup.Write(0, zapis);
            }
        }
        static void svetlo50()
        {
            zapis = (byte)(zapis & svetlodown);
            vystup.Write(0, zapis);
            Thread.Sleep(70);
            zapis = (byte)(zapis | svetloup);
            vystup.Write(0, zapis);
            Thread.Sleep(70);
        }
        static void svetlo100()
        {
            zapis = (byte)(zapis & svetlodown);
            vystup.Write(0, zapis);
            Thread.Sleep(5);
        }
        static void svetlo0()
        {
            zapis = (byte)(zapis | svetloup);
            vystup.Write(0, zapis);
            Thread.Sleep(50);
        }
        
    }
}
