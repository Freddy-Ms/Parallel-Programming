using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zad3
{
    class Program
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

        static string[] statusGornikow;

        static void Main(string[] args)
        {
            int[] liczbyGornikow = { 1, 2, 3, 4, 5, 6 };
            double[] czasy = new double[liczbyGornikow.Length];

            Console.WriteLine("Symulacja sprawności kopalni\n");

            for (int i = 0; i < liczbyGornikow.Length; i++)
            {
                int n = liczbyGornikow[i];
                Console.WriteLine($"\n=== Start symulacji dla {n} górników ===");

                Stopwatch sw = Stopwatch.StartNew();
                UruchomSymulacje(n);
                sw.Stop();

                czasy[i] = sw.Elapsed.TotalSeconds;
                Console.WriteLine($"Czas symulacji: {czasy[i]:F2} s\n");
            }

            Console.WriteLine("\n=== Wyniki zbiorcze ===");
            double czas1 = czasy[0];

            Console.WriteLine($"{"Liczba górników",-15} {"Czas [s]",-12} {"Przyśpieszenie",-15} {"Efektywność",-15}");
            for (int i = 0; i < liczbyGornikow.Length; i++)
            {
                int n = liczbyGornikow[i];
                double speedup = czas1 / czasy[i];
                double efficiency = speedup / n;
                Console.WriteLine($"{n,-15} {czasy[i],-12:F5} {speedup,-15:F5} {efficiency,-15:F5}");
            }
        }

        static void UruchomSymulacje(int liczbaGornikow)
        {
            semaforZloze = new SemaphoreSlim(2, 2);
            semaforMagazyn = new SemaphoreSlim(1, 1);
            statusGornikow = new string[liczbaGornikow];
            zlozo = 2000;
            magazyn = 0;

            Task[] gornicy = new Task[liczbaGornikow];

            for (int i = 0; i < liczbaGornikow; i++)
            {
                int id = i + 1;
                gornicy[i] = Task.Run(() => PracaGornika(id));
            }

            Task.WaitAll(gornicy);
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

                for (int i = 0; i < wydobyte; i++)
                    Thread.Sleep(czasWydobyciaJednostki);
                semaforZloze.Release();

                Thread.Sleep(czasPrzejazdu);

                semaforMagazyn.Wait();
                for (int i = 0; i < wydobyte; i++)
                    Thread.Sleep(czasRozladunkuJednostki);

                lock (lockObject)
                {
                    magazyn += wydobyte;
                }
                semaforMagazyn.Release();

                Thread.Sleep(czasPrzejazdu);

                lock (lockObject)
                {
                    if (zlozo <= 0) return;
                }
            }
        }
    }
}
