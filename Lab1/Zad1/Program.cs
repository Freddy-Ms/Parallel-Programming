using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zad1
{
    class Program
    {
        static int zlozo = 2000;
        static int pojemnoscPojazdu = 200;
        static int czasWydobyciaJednostki = 10;
        static int czasRozladunkuJednostki = 10;
        static int czasPrzejazdu = 10000;

        static SemaphoreSlim semaforZloze = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaforMagazyn = new SemaphoreSlim(1, 1);
        static object lockObject = new object();

        static void Main(string[] args)
        {
            int liczbaGornikow = 5;
            Task[] gornicy = new Task[liczbaGornikow];

            Console.WriteLine("=== Symulacja rozpoczęta ===");

            for (int i = 0; i < liczbaGornikow; i++)
            {
                int id = i + 1;
                gornicy[i] = Task.Run(() => PracaGornika(id));
            }

            Task.WaitAll(gornicy);

            Console.WriteLine("=== Symulacja zakończona ===");
        }

        static void PracaGornika(int id)
        {
            while (true)
            {
                semaforZloze.Wait();
                int wydobyte = 0;

                lock (lockObject)
                {
                    if (zlozo <= 0)
                    {
                        semaforZloze.Release();
                        return;
                    }

                    int doWydobycia = Math.Min(pojemnoscPojazdu, zlozo);
                    zlozo -= doWydobycia;
                    wydobyte = doWydobycia;
                }

                Console.WriteLine($"Górnik {id} wydobył {wydobyte} jednostek węgla. Pozostało w złożu: {zlozo} jednostek.");
                for (int i = 0; i < wydobyte; i++)
                {
                    Thread.Sleep(czasWydobyciaJednostki);
                }

                semaforZloze.Release();

                Console.WriteLine($"Górnik {id} transportuje węgiel do magazynu...");
                Thread.Sleep(czasPrzejazdu);

                semaforMagazyn.Wait();
                Console.WriteLine($"Górnik {id} rozładowuje węgiel...");
                for (int i = 0; i < wydobyte; i++)
                {
                    Thread.Sleep(czasRozladunkuJednostki);
                }
                semaforMagazyn.Release();

                Thread.Sleep(czasPrzejazdu);
            }
        }
    }
}
