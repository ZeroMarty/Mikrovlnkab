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
    8 -> 11 - P1.4 -> P1.7 - pozice na displeji (zleva doprava - A1 -> A4
    12 - výstup klávesnice

    znaky(hex adresy):
    1 - P2.0 - A
    2 - P2.1 - B
    3 - P2.2 - C
    4 - P2.3 - D
    5 - P2.4 - E

displej ovládej pomocí přepínání mezi segmenty.
 */
namespace Mikrovlnkab
{
    internal class Program
    {
        static DeviceInformation zarizeni = new DeviceInformation();
        static InstantDoCtrl vystup = new InstantDoCtrl();
        static InstantDiCtrl vstup = new InstantDiCtrl();
        static byte segment1 = 0b01111111;
        static byte segment2 = 0b10111111;
        static byte segment3 = 0b11011111;
        static byte segment4 = 0b11101111;
        static byte zamek = 0b11110111;
        static byte zvukdown = 0b11111011;
        static byte zvukup = 0b00000100;
        static byte motordown = 0b11111101;
        static byte motorup = 0b00000010;
        static byte svetloup = 0b11111110;
        static byte svetlodown = 0b00000001;
        static byte zapis = 0xff;
        static void Main(string[] args) //kód musí běžet neustále!
        {
            setup();
            zvuk();
            while(true)
            {

            }
        }
        static void setup()
        {
            zarizeni.Description = "PCI-1756,BID#0";
            zarizeni.DeviceMode = AccessMode.ModeWrite;
            vystup.SelectedDevice = zarizeni;
            vstup.SelectedDevice = zarizeni;
        }

        static void beh() //změnou střídy lze měnit rychlost otáčení a intenzitu světla
        {

        }
        static void zmena()
        {

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
