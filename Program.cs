using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automation.BDaq;
using System.IO;
using System.Threading;
using System.Diagnostics;
/*
 Schéma zapojení
  Levá - mikrovlnka, pravá - IO karta
    24 - GND - GND
    16 - P1.0 - Rozsvícení
    19 - P1.1 - Roztočení
    15 - P1.2 - Zvuk
    20 - P1.3 - Zámek
    7 - P3.0 - dveřní snímač
    12 - P3.1 - výstupní klávesnice
    8 -> 11 - P1.4 -> P1.7 - pozice na displeji (zleva doprava - A1 -> A4
    12 - výstup klávesnice

    znaky(hex adresy):
    1 - P2.0 - A
    2 - P2.1 - B
    3 - P2.2 - C
    4 - P2.3 - D
    5 - P2.4 - E

displej ovládej pomocí přepínání mezi segmenty.

bitové hodnoty čísel na displeji - spodních 5 bitů na portu 2
0 - 9 binárně - log.1 = cislo

konzole bude použitá jako stavový řádek 

nábéh světla - ~820ms
 */
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
        static byte segmentnull = 0b11100000;
        static byte[] cisla = new byte[]
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
        static byte zamekup = 0b11110111;
        static byte zamekdown = 0b00001000;
        static byte zvukdown = 0b11111011;
        static byte zvukup = 0b00000100;
        static byte motordown = 0b11111101;
        static byte motorup = 0b00000010;
        static byte svetlodown = 0b11111110;
        static byte svetloup = 0b00000001;//invertuj proměnné, takhle rozpohybujou všechno
        static byte zapis = 0xff; //všeobecné výstupy
        static byte zapis2 = 0xff; //adresa prom
        static int kod = 0;
        static List <int> zamek = new List <int>();
        static List<int> klic = new List<int>();
        static bool jekod = false;
        
        static void Main(string[] args) //kód musí běžet neustále!
        {
            setup();
            //zvuk();
            //beh();
            while(true)
            {
                vstup.Read(0, out byte info);
                if((info & 1 << 0) != 0) //alarm u dveří
                {
                    Console.WriteLine("Dveře jsou otevřené");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while(sw.Elapsed.Seconds < 60) //potom přenastav na 60 sekund
                    {
                        vstup.Read(0, out info);
                        if((info & 1 << 0) == 0)
                        {
                            Console.WriteLine("Dveře jsou zavřené");
                            break;
                        }
                    }
                    zvuk();
                }
                Console.WriteLine("podmínka doběhla");
                if ((info & 1 << 0) == 0)
                {
                    if (jekod == false)// zadání kódu
                    {

                        Console.WriteLine("Zapsání kódu \n");
                        Console.WriteLine("5 sekund prodleva po každé číslici, je zapotřebí u každé části kódu psát jiným tlačítkem\n");
                        for (int i = 0; i < 4; i++)
                        {
                            byte operace = zapnutisegmentu(i);

                            zapis = (byte)(zapis | segmentoff);
                            zapis = (byte)(zapis & operace);
                            vystup.Write(0, zapis);
                            Thread.Sleep(20);
                            byte display = nastavenisegmentu(kod);
                            zapis2 = (byte)(zapis2 & segmentnull);
                            zapis2 = (byte)(zapis2 | display);
                            vystup.Write(1, zapis2);
                        
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            Console.WriteLine("Měřim čas");
                            while (sw.Elapsed.Seconds < 5)
                            {
                                
                                vstup.Read(0, out byte data);
                                if ((data & 1 << 1) == 0)
                                {
                                    Console.WriteLine("Píšu kód");
                                    kod++;
                                    if (kod > 9)
                                    {
                                        kod = 0;
                                    }
                                    display = nastavenisegmentu(kod);
                                    zapis2 = (byte)(zapis2 & segmentnull);
                                    zapis2 = (byte)(zapis2 | display);
                                    vystup.Write(1, zapis2);
                                    Thread.Sleep(20);
                                    Thread.Sleep(200);
                                    sw.Restart();
                                }

                            }
                            sw.Reset();
                            zamek.Add(kod);
                            kod = 0;
                        }
                        using (StreamWriter writer = new StreamWriter(cesta))
                        {
                            foreach (int i in zamek)
                            {
                                writer.Write(i);
                            }
                        }
                        jekod = true;
                        zamek.Clear();
                    }
                    else if (jekod == true) //Hádání kódu
                    {
                        int pocitadlo = 0;
                        using (StreamReader reader = new StreamReader(cesta))
                        {
                            string kod = reader.ReadToEnd();
                            foreach(char znak in kod)
                            {
                                int cislo = (int)znak;
                                zamek.Add(cislo);
                            }   
                        }
                        while(jekod == true )
                        {
                            for(int i = 0; i < 4; i++)
                            {
                                kod = 0;
                                Console.WriteLine("Hádání kódu \n");
                                Console.WriteLine("5 sekund prodleva po každé číslici, je zapotřebí u každé části kódu psát jiným tlačítkem\n");
                                byte operace = zapnutisegmentu(i);
                                zapis = (byte)(zapis | segmentoff);
                                zapis = (byte)(zapis & operace);
                                vystup.Write(0, zapis);
                                Thread.Sleep(20);
                                byte display = nastavenisegmentu(kod);
                                zapis2 = (byte)(zapis2 & segmentnull);
                                zapis2 = (byte)(zapis2 | display);
                                vystup.Write(1, zapis2);
                                Stopwatch sw = new Stopwatch();
                                sw.Start();
                                Console.WriteLine("Měřim čas");
                                while (sw.Elapsed.Seconds < 5)
                                {

                                    vstup.Read(0, out byte data);
                                    if ((data & 1 << 1) == 0)
                                    {
                                        Console.WriteLine("Píšu kód");
                                        kod++;
                                        if (kod > 9)
                                        {
                                            kod = 0;
                                        }
                                        display = nastavenisegmentu(kod);
                                        zapis2 = (byte)(zapis2 & segmentnull);
                                        zapis2 = (byte)(zapis2 | display);
                                        vystup.Write(1, zapis2);
                                        Thread.Sleep(20);
                                        Thread.Sleep(200);
                                        sw.Restart();
                                    }

                                }
                                sw.Reset();
                                klic.Add(kod);
                            }

                            bool otevrise = odemknuti(klic, zamek);

                            if(otevrise == true)
                            {
                                zapis = (byte)(zapis & zamekdown);
                                vystup.Write(0, zapis);
                                Thread.Sleep(50);
                                zapis = (byte)(zapis | zamekup);
                                vystup.Write(0, zapis);
                                Thread.Sleep(50);
                                klic.Clear();
                                zamek.Clear();
                                jekod = false;
                                File.Delete(cesta);
                            }
                            else if(otevrise == false)
                            {
                                pocitadlo++;
                                klic.Clear();
                            }
                            
                            if (pocitadlo == 3)
                            {
                                zvuk();
                                pocitadlo = 0;
                            }
                        }
                        
                    }
                }
                
                
            }
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
            Console.WriteLine($"Existuje kód: {jekod}");
        }
        static void beh(/*int cas*/) //změnou střídy lze měnit rychlost otáčení a intenzitu světla
        {
            zapis = (byte)(zapis & zamekdown);
            zapis = (byte)(zapis & svetlodown);
            vystup.Write(0, zapis);
            for (int i = 0; i < 500; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.ElapsedMilliseconds < 3)
                {

                }
                zapis = (byte)(zapis & motordown);
                ;
                vystup.Write(0, zapis);
                sw.Reset();
                sw.Start();
                while (sw.ElapsedMilliseconds < 3)
                {

                }
                zapis = (byte)(zapis | motorup);
                vystup.Write(0, zapis);
            }
            zapis = (byte)(zapis  | svetloup);
            vystup.Write(0,zapis);
        }
        
        
        
        static byte nastavenisegmentu(int cislo)
        {
            byte operace = cisla[cislo];
            return operace;
        }

        static bool odemknuti(List<int> a, List<int> b)
        {
            int spravne = 0;
            for(int i = 0;i<4;i++)
            {
                if (a[i] == b[i])
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
        /*static void svetlo50()
        {
            for (int i = 0; i < 420; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.ElapsedMilliseconds < 5)
                {

                }
                zapis = (byte)(zapis & svetlodown);
                vystup.Write(0, zapis);
                sw.Reset();
                sw.Start();
                while (sw.ElapsedMilliseconds < 5)
                {

                }
                zapis = (byte)(zapis | svetloup);
                vystup.Write(0, zapis);
            }
        }*/
        
    }
}
