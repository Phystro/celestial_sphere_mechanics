using System;
using System.Threading;
using System.Threading.Tasks;

namespace LonLatLibrary
{
	public interface IGlobe
	{
		double Radius{ get; set; }
		string AddSuffix(string lname);
		double Haversine(double lon1, double lat1, double lon2, double lat2);
		double Haversine(Coordinate locationA, Coordinate locationB);
	}


	public struct SubCoordinate
	{
		public int Degrees { get; set; }
		public int Minutes { get; set; }
		public double Seconds { get; set; }
		public CardinalPoint Direction{ get; set; }

		public double DecimalDegrees
		{ 
			get
			{
				double result = Degrees;
				result += (Minutes/60.0) + (Seconds/3600.0);
				if (Direction == CardinalPoint.North || Direction == CardinalPoint.East){
					return result;
				} else{
					return -result;
				}
			}
		}

		public double DecimalRadians => Globe.Deg2Rad(DecimalDegrees);

		public static SubCoordinate Parse(string input)
		{
			// 32*12'21" W
			CardinalPoint direction;

			if (input.EndsWith('W')){
				direction = CardinalPoint.West;
			} else if (input.EndsWith('E')){
				direction = CardinalPoint.East;
			} else if (input.EndsWith('N')){
				direction = CardinalPoint.North;
			} else if (input.EndsWith('S')){
				direction = CardinalPoint.South;
			} else{
				throw new FormatException("Invalid or Unspecified direction");
			}

			//input = input.Substring(0, input.Length - 1);
			input = input[0..^1];
			input = input.Trim();
			input = input.Replace('*', '.');
			input = input.Replace('\'', '.');
			input = input.Replace('\"', '.');

			string[] values = input.Split('.');
			if ( !(values.Length == 3 || values.Length == 4) )
			{
				throw new FormatException("Invalid input");
			}


			int degrees = int.Parse(values[0]);
			int minutes = int.Parse(values[1]);
			double seconds;

			if (values.Length == 3){
				seconds = double.Parse(values[2]);
			} else{
				seconds = double.Parse(string.Join('.', values[2], values[3]));
			}

			return new SubCoordinate()
			{
				Direction = direction,
				Degrees = degrees,
				Minutes = minutes,
				Seconds = seconds
			};
		}

		public static bool TryParse(string input, out SubCoordinate output)
		{
			output = default;
			try
			{
				output = Parse(input);
				return true;	
			}
			catch (FormatException)
			{
				return false;
			}
		}
	}

	public struct Coordinate
	{
		public SubCoordinate Longitude{ get; set; }
		public SubCoordinate Latitude{ get; set; }

		public Tuple<double, double> DecimalDegrees => new Tuple<double, double> (Longitude.DecimalDegrees, Latitude.DecimalDegrees);
		public Tuple<double, double> DecimalRadians => new Tuple<double, double> (Longitude.DecimalRadians, Latitude.DecimalRadians);

		public static Coordinate Parse(string longitude, string latitude)
		{
			SubCoordinate lon = SubCoordinate.Parse(longitude);
			SubCoordinate lat = SubCoordinate.Parse(latitude);

			return new Coordinate(){
				Longitude = lon,
				Latitude = lat
			};
		}

		public static bool TryParse(string longitude, string latitude, out Coordinate output)
		{
			output = default;
			try
			{
				output = Parse(longitude, latitude);
				return true;	
			}
			catch (FormatException)
			{
				return false;
			}
		}
	}

	public enum CardinalPoint
	{ 
		North,
		South,
		East,
		West
	}


	public class Globe : IGlobe
	{
		public const double G = 6.6743015e-11;
		public double Radius{ get; set; }
		public double Mass{ get; set; }

		public static Globe Earth => new Globe(6367.0, 5.97237e24);
		public static Globe Pluto => new Globe(1188.3, 1.30900e22);

		public Globe()
		{

		}

		public Globe(double distanceRadial, double mass)
		{
			Radius = distanceRadial;
			Mass = mass;
		}

		public string AddSuffix(string lname){
			string suffix = "\t distance between two points on the globe in meters";
			string fname = lname + suffix;
			return fname;
		}

		public static double Deg2Rad(double deg)
		{
			return deg * (Math.PI / 180.0);
		}


		public double Haversine(double lon1, double lat1, double lon2, double lat2){
			
			// Haversine formula
			double dlon = lon2 - lon1;
			double dlat = lat2 - lat1;

			double a = Math.Pow(Math.Sin(dlat/2.0), 2)
			       	+ Math.Cos(lat1) 
				* Math.Cos(lat2)
				* Math.Pow(Math.Sin(dlon/2.0), 2);
			
			double c = 2 * Math.Asin(Math.Sqrt(a));

			double km = Radius * c;
			return km;
		}

		public double Haversine(Coordinate locationA, Coordinate locationB)
		{
			(double lon1, double lat1) = locationA.DecimalRadians;
			(double lon2, double lat2) = locationB.DecimalRadians;

			return Haversine(lon1, lat1, lon2, lat2);
		}



		public double ForceGravity(double massBody, double distanceRadial, bool isDistanceFromCenter=true)
		{
			if (!isDistanceFromCenter){
				distanceRadial += Radius;
			}

			double a = massBody * Mass;
			double b = distanceRadial * distanceRadial;
			double c = a/b;
			double d = G * c;

			return d;
		}

		public double AccelerationGravity(double massBody, double distanceRadial, bool isDistanceFromCenter=true)
		{
			return ForceGravity(massBody, distanceRadial, isDistanceFromCenter) / massBody;
		}

		public double AccelerationGravityOnSurface(double massBody)
		{
			return AccelerationGravity(massBody, Radius);
		}


		//
		public async Task<double> TimeToImpactAsync(double massBody, double distanceRadial, double punjepunje, double initVelocity, IProgress<double> simulationProgress=null, CancellationToken cancellationToken=default)
		{
			double remDistance = distanceRadial;
			double eta = 0;

			await Task.Run( () => 
			{
				while (true)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						cancellationToken.ThrowIfCancellationRequested();
						break;
					}

					double a = AccelerationGravity(massBody, remDistance, false);
					double t = ( Math.Sqrt( Math.Pow(initVelocity, 2)  + (2 * a * punjepunje) ) - initVelocity ) / a;
					remDistance -= punjepunje;
					initVelocity += a * t;
					eta += t;

					if (simulationProgress != null)
					{
						simulationProgress.Report(1 - (remDistance / distanceRadial ));
					}

					if (remDistance <= 0){
						break;
					}
				}
			} ).ConfigureAwait(false);

			return eta;
		}

	}

}

