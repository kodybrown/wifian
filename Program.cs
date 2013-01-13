//
// Copyright (C) 2013 Kody Brown (kody@bricksoft.com).
// 
// MIT License:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NativeWifi;

namespace WifiAnalyzer
{
	public class Program
	{
		public static void Main( string[] args )
		{
			WlanClient client = new WlanClient();
			bool exit = false;
			int timeout = 250;
			int ethIndex = 0;

			ConsoleColor defaultColor = Console.ForegroundColor;
			ConsoleColor highlightColor = ConsoleColor.Cyan;

			List<Wlan.WlanBssEntry> wlanBssEntries = new List<Wlan.WlanBssEntry>();
			ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();
			Dictionary<int, string> ethNames = new Dictionary<int, string>();

			foreach (WlanClient.WlanInterface wlanIface in client.Interfaces) {
				ethNames.Add(0, wlanIface.InterfaceName);
			}

			try {
				while (true) {
					Thread.Sleep(timeout);

					Console.Clear();
					wlanBssEntries.Clear();
					ethIndex = 0;

					Console.WriteLine("Signal Strength Analyzer. Copyright (C) 2013 Kody Brown.");
					Console.WriteLine("This software is in the public domain, without warranty.");
					Console.WriteLine();
					Console.WriteLine("Last update: {0}", DateTime.Now.ToString("hh:mm:ss"));
					Console.WriteLine("Press [Space] to pause or [Escape] to quit.");

					foreach (WlanClient.WlanInterface wlanIface in client.Interfaces) {
						Console.ForegroundColor = highlightColor;
						Console.WriteLine("\neth" + ethIndex + " \"" + ethNames[ethIndex] + "\":");
						//Console.WriteLine(wlanIface.InterfaceName + ":");
						ethIndex++;
						Console.ForegroundColor = defaultColor;

						wlanBssEntries.AddRange(wlanIface.GetNetworkBssList());

						wlanBssEntries.Sort(delegate( Wlan.WlanBssEntry a, Wlan.WlanBssEntry b )
						{
							return string.Compare(ASCIIEncoding.ASCII.GetString(a.dot11Ssid.SSID),
								ASCIIEncoding.ASCII.GetString(b.dot11Ssid.SSID), StringComparison.InvariantCultureIgnoreCase);
						});

						foreach (Wlan.WlanBssEntry network in wlanBssEntries) {
							WriteNetwork(network);
						}

						if (Console.KeyAvailable) {
							keyPressed = Console.ReadKey(true);
							if (keyPressed.Key == ConsoleKey.Q || keyPressed.Key == ConsoleKey.Escape) {
								exit = true;
								break;
							}
						}
					}

					if (exit) {
						break;
					}
					if (keyPressed.Key == ConsoleKey.P || keyPressed.Key == ConsoleKey.Spacebar) {
						Console.WriteLine("\nPAUSED: Press any key to continue");
						keyPressed = Console.ReadKey(true);
						if (keyPressed.Key == ConsoleKey.Q || keyPressed.Key == ConsoleKey.Escape) {
							exit = true;
							break;
						}
						keyPressed = new ConsoleKeyInfo();
					}
				}

			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine("Press any key to exit");
				Console.ReadKey(true);
			}

			Console.ForegroundColor = defaultColor;
		}

		private static void WriteNetwork( Wlan.WlanBssEntry network )
		{
			int mzxSsidLen = 22;
			int barLength = 25;
			string format = "  [{0,-" + mzxSsidLen + "}][Signal:{1,-3:###}dBm][{2}]{3}%";
			string ssidName = ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).Trim('\0');

			if (ssidName.Length > mzxSsidLen) {
				ssidName = ssidName.Substring(0, mzxSsidLen).Trim();
			} else if (ssidName.Length == 0) {
				ssidName = "<unknown/hidden>";
			}

			Console.WriteLine(format, ssidName, network.rssi, CreateBar(network, barLength), network.linkQuality);
		}

		private static string CreateBar( Wlan.WlanBssEntry network, int barLength )
		{
			float barWidth = (float)barLength / 100;
			uint linkQuality = network.linkQuality;
			int barValue = (int)(linkQuality * barWidth);

			// max == 50

			// '▄' '▐' '█'
			return string.Format("{0}{1}", new string('█', barValue), new string(' ', barLength - barValue));
		}
	}
}
