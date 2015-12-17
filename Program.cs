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
		private static ConsoleColor defaultColor = Console.ForegroundColor;
		private static ConsoleColor highlightColor = ConsoleColor.Cyan;
		private static ConsoleColor errorColor = ConsoleColor.Red;
		private static ConsoleColor indexColor = ConsoleColor.DarkGray;

		private static ConsoleColor color1 = ConsoleColor.Green;
		private static ConsoleColor color2 = ConsoleColor.Green;
		private static ConsoleColor color3 = ConsoleColor.DarkGreen;
		private static ConsoleColor color4 = ConsoleColor.DarkGreen;
		private static ConsoleColor color5 = ConsoleColor.Red;
		private static ConsoleColor color6 = ConsoleColor.DarkRed;

		public enum SortBy
		{
			NotSet = 0,
			Name,
			Signal
		}

		public static void Main( string[] args )
		{
			WlanClient client = new WlanClient();
			bool exit = false;
			int timeout = 500;
			int ethIndex = 0;
			int netIndex = 0;

			SortBy sortBy = SortBy.Name;
			bool shift = false;
			int sortModifier = -1;

			List<Wlan.WlanBssEntry> wlanBssEntries = new List<Wlan.WlanBssEntry>();
			ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();
			Dictionary<int, string> ethNames = new Dictionary<int, string>();

			// '┼' '─'
			string captionSeparator = string.Format("────{0,-" + colSsid + "}─┼─{1,-" + colSignal + "}─┼─{2}──────"
				, new string('─', colSsid), new string('─', colSignal), new string('─', colBarLen));

			ethIndex = 0;
			foreach (WlanClient.WlanInterface wlanIface in client.Interfaces) {
				// I store the interface name here because it is pretty slow (>500ms) to retrieve it..
				// Hopefully, the indexes won't change...
				if (wlanIface != null && wlanIface.InterfaceName != null) {
					ethNames.Add(ethIndex++, wlanIface.InterfaceName);
				}
			}

			try {
				while (true) {
					Thread.Sleep(timeout);

					Console.Clear();
					ethIndex = 0;

					Console.WriteLine("wifian - WiFi Signal Strength Analyzer. Copyright (C) 2013 Kody Brown.");
					Console.WriteLine("See github.com/kodybrown/wifian for licensing details (MIT License).");
					Console.Write("{0," + (Console.WindowWidth - 2) + "}", string.Format("Last update: {0}", DateTime.Now.ToString("hh:mm:ss")));

					//foreach (WlanClient.WlanInterface wlanIface in client.Interfaces) {
					for (ethIndex = 0; ethIndex < client.Interfaces.Length; ethIndex++) {
						WlanClient.WlanInterface wlanIface = client.Interfaces[ethIndex];

						Console.ForegroundColor = highlightColor;
						Console.WriteLine("\neth" + ethIndex + " \"" + ethNames[ethIndex] + "\":");
						//Console.WriteLine(wlanIface.InterfaceName + ":");
						ethIndex++;
						Console.ForegroundColor = defaultColor;
						Console.WriteLine("    {0,-" + colSsid + "}   {1,-" + colSignal + "}   {2}", "[S]SID" + (sortBy == SortBy.Name ? (shift ? " «" : " »") : ""), "Signal", "Signal S[t]rength" + (sortBy == SortBy.Signal ? (shift ? " «" : " »") : ""));
						Console.WriteLine(captionSeparator);

						wlanBssEntries.Clear();
						wlanBssEntries.AddRange(wlanIface.GetNetworkBssList());

						if (sortBy == SortBy.Name) {
							sortModifier = shift ? -1 : 1;
							wlanBssEntries.Sort(delegate ( Wlan.WlanBssEntry a, Wlan.WlanBssEntry b ) {
								return sortModifier * string.Compare(ASCIIEncoding.ASCII.GetString(a.dot11Ssid.SSID),
									ASCIIEncoding.ASCII.GetString(b.dot11Ssid.SSID), StringComparison.InvariantCultureIgnoreCase);
							});
						} else if (sortBy == SortBy.Signal) {
							sortModifier = shift ? 1 : -1;
							wlanBssEntries.Sort(delegate ( Wlan.WlanBssEntry a, Wlan.WlanBssEntry b ) {
								return sortModifier * a.rssi.CompareTo(b.rssi);
							});
						}

						netIndex = 0;
						foreach (Wlan.WlanBssEntry network in wlanBssEntries) {
							WriteNetwork(++netIndex, network);
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

					if (keyPressed.Key == ConsoleKey.S) {
						if (sortBy != SortBy.Name) {
							// switching from a different sort column
							shift = false;
						} else {
							shift = !shift;
						}
						sortBy = SortBy.Name;
						keyPressed = new ConsoleKeyInfo();
					} else if (keyPressed.Key == ConsoleKey.T) {
						if (sortBy != SortBy.Signal) {
							// switching from a different sort column
							shift = false;
						} else {
							shift = !shift;
						}
						sortBy = SortBy.Signal;
						keyPressed = new ConsoleKeyInfo();
					} else if (keyPressed.Key == ConsoleKey.P || keyPressed.Key == ConsoleKey.Spacebar) {
						Console.Write("\nPAUSED: Press any key to continue");
						keyPressed = Console.ReadKey(true);
						if (keyPressed.Key == ConsoleKey.Q || keyPressed.Key == ConsoleKey.Escape) {
							exit = true;
							break;
						}
						keyPressed = new ConsoleKeyInfo();
					} else {
						//Console.WriteLine("\nPress [S] to change sort and [R] to reverse.");
						Console.WriteLine("\nPress [Space] to pause or [Escape] to quit.");
					}
				}

			} catch (Exception ex) {
				Console.ForegroundColor = errorColor;
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine("\nPlease copy and send this error to kody@bricksoft.com! Thank you!");
				Console.WriteLine("Press any key to exit");
				Console.ReadKey(true);
			}

			Console.ForegroundColor = defaultColor;
		}

		private static int colSsid = 25;
		private static int colSignal = 8;
		private static int colBarLen = 25;

		private static void WriteNetwork( int netIndex, Wlan.WlanBssEntry network )
		{
			string ssidName = ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).Trim('\0');
			string bar = CreateBar(network, colBarLen);

			if (ssidName.Length > colSsid) {
				ssidName = ssidName.Substring(0, colSsid).Trim();
			} else if (ssidName.Length == 0) {
				ssidName = "<unknown/hidden>";
			}

			Console.ForegroundColor = indexColor;
			Console.Write("{0,-3}", netIndex);
			Console.ForegroundColor = defaultColor;
			Console.Write(" {0,-" + colSsid + "} │ {1," + colSignal + ":###} │ ", ssidName, network.rssi + " dBm");

			if (network.linkQuality > 85) {
				Console.ForegroundColor = color1;
			} else if (network.linkQuality > 65) {
				Console.ForegroundColor = color2;
			} else if (network.linkQuality > 50) {
				Console.ForegroundColor = color3;
			} else if (network.linkQuality > 35) {
				Console.ForegroundColor = color4;
			} else if (network.linkQuality > 20) {
				Console.ForegroundColor = color5;
			} else {
				Console.ForegroundColor = color6;
			}

			Console.Write(bar);

			Console.ForegroundColor = defaultColor;
			Console.WriteLine(" {0,3}%", network.linkQuality);
		}

		private static string CreateBar( Wlan.WlanBssEntry network, int barLength )
		{
			float barWidth = (float)barLength / 100;
			uint linkQuality = network.linkQuality;
			int barValue = (int)(linkQuality * barWidth);
			//return string.Format("{0}{1}", new string('▄', barValue), new string(' ', barLength - barValue));

			// '▄' '▀' '▐' '█' '≡' '═'
			char ch;

			//if (linkQuality > 70) {
			//	ch = '≡';
			//} else {
			//	ch = '═';
			//}
			ch = '═';

			return string.Format("{0}{1}", new string(ch, barValue), new string(' ', barLength - barValue));
		}
	}
}

