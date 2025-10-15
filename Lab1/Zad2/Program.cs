using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zad2
{
    class Zad2
    {
        static int zlozo = 2000;
        static int magazyn = 0;
        static int pojemnoscPojazdu = 200;
        static int czasWydobyciaJednostki = 10;
        static int czasRozladunkuJednostki = 10;
        static int czasPrzejazdu = 10000;

        static SemaphoreSlim semaforZloze = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaforMagazyn = new SemaphoreSlim(1, 1);
        static object lockObject = new object();
        static object consoleLock = new object();

        static string[] statusGornikow;

        static void Main(string[] args)
        {
            int liczbaGornikow = 5;
            statusGornikow = new string[liczbaGornikow];

            Console.CursorVisible = false;
            Console.Clear();

            Task monitor = Task.Run(() => MonitorujStan(liczbaGornikow));

            Task[] gornicy = new Task[liczbaGornikow];
            for (int i = 0; i < liczbaGornikow; i++)
            {
                int id = i + 1;
                gornicy[i] = Task.Run(() => PracaGornika(id));
            }

            Task.WaitAll(gornicy);

            lock (consoleLock)
            {
                Console.SetCursorPosition(0, 2 + liczbaGornikow);
                Console.WriteLine("=== Symulacja zakończona ===".PadRight(50));
            }

            Console.CursorVisible = true;
        }

        static void PracaGornika(int id)
        {
            while (true)
            {
                AktualizujStatus(id, "Jedzie do kopalni...");
                Thread.Sleep(czasPrzejazdu);
                AktualizujStatus(id, "Czeka na wydobycie...");
                semaforZloze.Wait();
                int wydobyte = 0;

                lock (lockObject)
                {
                    if (zlozo <= 0)
                    {
                        semaforZloze.Release();
                        AktualizujStatus(id, "Zakończył pracę.");
                        return;
                    }

                    int doWydobycia = Math.Min(pojemnoscPojazdu, zlozo);
                    zlozo -= doWydobycia;
                    wydobyte = doWydobycia;
                }

                AktualizujStatus(id, "Wydobywa węgiel...");
                for (int i = 0; i < wydobyte; i++)
                    Thread.Sleep(czasWydobyciaJednostki);

                semaforZloze.Release();

                AktualizujStatus(id, "Transportuje do magazynu...");
                Thread.Sleep(czasPrzejazdu);
                AktualizujStatus(id, "Czeka na pusty magazyn...");
                semaforMagazyn.Wait();
                AktualizujStatus(id, "Rozładowuje węgiel...");
                for (int i = 0; i < wydobyte; i++)
                    Thread.Sleep(czasRozladunkuJednostki);
                lock (lockObject)
                {
                    magazyn += wydobyte;
                }
                semaforMagazyn.Release();

            }
        }

        static void MonitorujStan(int liczbaGornikow)
        {
            while (true)
            {
                lock (consoleLock)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine($"Stan złoża: {zlozo} jednostek węgla".PadRight(50));
                    Console.WriteLine($"Stan magazynu: {magazyn} jednostek węgla".PadRight(50));

                    for (int i = 0; i < liczbaGornikow; i++)
                    {
                        Console.SetCursorPosition(0, i + 2);
                        Console.WriteLine($"Górnik {i + 1}: {statusGornikow[i]}".PadRight(50));
                    }
                }

                Thread.Sleep(200);
            }
        }

        static void AktualizujStatus(int id, string status)
        {
            statusGornikow[id - 1] = status;
        }
    }
}
