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
    14 - P3.1 - teplotní snímač
    11 - P3.2 - set
    10 - P3.3 - v
    9 - P3.4 - ^
    8 - P3.5 - MODE
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
            0b11101111, // segment vpravo
            0b11011111,
            0b10111111,
            0b01111111 //segment vlevo
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
        static byte zamekzamknout = 0b11110111;
        static byte zamekodemknout = 0b00001000;
        static byte zvukdown = 0b11111011;
        static byte zvukup = 0b00000100;
        static byte motordown = 0b11111101;
        static byte motorup = 0b00000010;
        static byte svetloup = 0b11111110;
        static byte svetlodown = 0b00000001;
        static byte zapis = 0xff; //všeobecné výstupy
        static byte zapis2 = 0xff; //adresa prom
        static int kod = 0;
        static int aktualnisegment = 0;
        static bool segmenton = false;
        static List <int> zamek = new List <int>();
        static bool jekod = false;
        
        static void Main(string[] args) //kód musí běžet neustále!
        {
            setup();
            //zvuk();
            beh();
            while(true)
            {
                if(jekod == false)// zadání kódu
                {
                    vstup.Read(0, out byte data);
                    if ((data & 1 << 5) == 0 && segmenton == false) //mode button -> zapnutí segmentovky.
                    {
                        segmenton = true;
                        aktualnisegment = 0;
                        zapis = (byte)(zapis & segmenty[0]);
                        vystup.Write(0, zapis);
                        Thread.Sleep(100);
                    }
                    else if ((data & 1 << 5) == 0 && segmenton == true)
                    {
                        segmenton = false;
                        zapis = (byte)(zapis | segmentoff);
                        vystup.Write(0, zapis);
                        Thread.Sleep(100);
                    }
                    if (segmenton == true)
                    {
                        if ((data & 1 << 4) == 0) // ^ button
                        {
                            kod++;
                            if (kod > 9)
                            {
                                kod = 0;
                            }
                            byte operace = nastavenisegmentu(kod);
                            zapis2 = (byte)(zapis2 & segmentnull);
                            zapis2 = (byte)(zapis2 | operace);
                            vystup.Write(0, zapis2);
                            Thread.Sleep(100);
                        }
                        if ((data & 1 << 3) == 0) // v button
                        {
                            kod--;
                            if (kod < 0)
                            {
                                kod = 9;
                            }
                            byte operace = nastavenisegmentu(kod);
                            zapis = (byte)(zapis & segmentnull);
                            zapis = (byte)(zapis | operace);
                            vystup.Write(0, zapis);
                            Thread.Sleep(100);
                        }
                        if ((data & 1 << 2) == 0) //set button -> posun a nastavení čísla do kódu
                        {
                            aktualnisegment++;
                            zapis = (byte)(zapis | segmentoff);
                            zapis = (byte)(zapis & segmenty[aktualnisegment]);
                            Thread.Sleep(100);
                            zamek.Add(kod);
                            kod = 0;
                            if (aktualnisegment == 4)
                            {
                                using (StreamWriter text = new StreamWriter(cesta))
                                {
                                    foreach (int i in zamek)
                                    {
                                        text.Write(i);
                                    }
                                }
                            }
                        }
                        if ((data & 1 << 0) != 0) //dveře
                        {
                            for (int i = 0; i <= 60000; i++)
                            {
                                vstup.Read(0, out byte dvere);
                                if (dvere != 0)
                                {
                                    break;
                                }
                                else //pokud budou dveře otevřeny minutu -> zvuk
                                {
                                    if (i == 60000)
                                    {
                                        zvuk();
                                    }
                                    Thread.Sleep(1);
                                }
                            }
                        }
                    }
                }
                else if(jekod == true) //Zadání kódu
                {

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
        }
        static void beh(/*int cas*/) //změnou střídy lze měnit rychlost otáčení a intenzitu světla
        {
            zapis = (byte)(zapis & zamekzamknout);
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
            zapis = (byte)(zapis | zamekodemknout | svetloup);
            vystup.Write(0,zapis);
        }
        
        
        
        static byte nastavenisegmentu(int cislo)
        {
            byte operace = cisla[cislo];
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
        static void hodiny()
        {
            
        }
    }
}
