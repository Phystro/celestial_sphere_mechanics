using System;
using System.Threading.Tasks;

namespace LonLatLibrary
{

	/// <summary>
    /// A simple static class that can be used to tell the interval between two periods.
    /// </summary>
    public static class DeltaTime
    {
        static DeltaTime()
        {
            Capture();
        }
        private static DateTime _now;

        /// <summary>
        /// Sets the delta time object to start counting the intervals
        /// </summary>
        public static void Capture()
        {
            _now = DateTime.Now;
        }

        /// <summary>
        /// Returns the time that has elapsed in miliseconds since the last <see cref="Capture"/> was called
        /// </summary>
        public static double Elapsed
        {
            get=> (DateTime.Now - _now).TotalMilliseconds;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
			Globe g = Globe.Earth;
			
			string location_name = "Nowhere";
			Console.WriteLine(g.AddSuffix(location_name));

			// string lon, lat;
			// Coordinate A, B;

			// Console.WriteLine("Location A:");
			// Console.Write("longitude:\t");
			// lon = Console.ReadLine();
			// Console.Write("latitude:\t");
			// lat = Console.ReadLine();
			// A = Coordinate.Parse(lon, lat);

			// Console.WriteLine("Location B:");
			// Console.Write("longitude:\t");
			// lon = Console.ReadLine();
			// Console.Write("latitude:\t");
			// lat = Console.ReadLine();
			// B = Coordinate.Parse(lon, lat);

			// double distance = g.Haversine(A, B);
			// Console.WriteLine($"Distance:\t{distance} km\n\n");


			//
			// double massAsteroid = 6.82e15;
			double massMoon = 7.342e22;
			double distanceRadial = 384400000;
			Console.WriteLine("Starting Simulation");
			Console.WriteLine(
				$"Force of Gravity: {g.ForceGravity(massMoon, distanceRadial, false)}\n"
				+$"Acceleration due to Gravity: {g.AccelerationGravity(massMoon, distanceRadial, false)}\n"
				+$"Acceleration due to Gravity on the surface: {g.AccelerationGravityOnSurface(massMoon)}"
			);

			// Progress<double> progress = new Progress<double>();
			// progress.ProgressChanged += ( o, e ) => { Console.WriteLine($"{e*100}%\r"); };

			double eta_seconds = await g.TimeToImpactAsync(massMoon, distanceRadial, 1, 0.0, WriteProgress());
			//DateTime dateTime = DateTime.MinValue.AddSeconds(eta);
			long ticks = (long)(eta_seconds * 1e9);
			TimeSpan eta = new TimeSpan(ticks);

			Console.WriteLine($"Time to Impact: {eta.ToString(@"dd\:hh\:mm\:ss")}");
		}

		public static Progress<double> WriteProgress()
        {
            var progress = new Progress<double>();

            int previousProgress = 0;

            DeltaTime.Capture();

            Console.WriteLine();

            progress.ProgressChanged += (o, e) =>
            {
                var er = Math.Round(e, 2);
                if (DeltaTime.Elapsed < 100 && er != 1) return;

                DeltaTime.Capture();
                int percent = (int)(er * 100.0);

                if (percent - previousProgress < 1) return;

                previousProgress = percent;

                
                string progBar = "Execution Progress: ";
                for (int i = 0; i <= 100; i += 10)
                {
                    if (i <= percent) progBar += "|";
                    else progBar += ".";
                }
                Console.Write("\r");
                progBar += $" {percent}%";

                Console.Write(progBar);

                if (percent == 100)
                {
                    Console.WriteLine();
                }
            };

            return progress;
        }

    }
}
