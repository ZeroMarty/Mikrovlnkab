using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automation.BDaq;
using System.IO;
using System.Threading;

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
    8 -> 11 - P1.3 -> 6 - pozice na displeji (zleva doprava - A1 -> A4
    12 - výstup klávesnice

    znaky(hex adresy):
    1 - P2.0 - A
    2 - P2.1 - B
    3 - P2.2 - C
    4 - P2.3 - D
    5 - P2.4 - E
 */
namespace Mikrovlnkab
{
    internal class Program
    {

        static void Main(string[] args)
        {
            while(true)
            {

            }
        }
        public void beh() //změnou střídy lze měnit rychlost otáčení a intenzitu světla
        {

        }
    }
}
