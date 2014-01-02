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
		private static ConsoleColor color2 = ConsoleColor.DarkGreen;
		private static ConsoleColor color3 = ConsoleColor.Magenta;
		private static ConsoleColor color4 = ConsoleColor.Red;
		private static ConsoleColor color5 = ConsoleColor.DarkRed;

		public static void Main( string[] args )
		{
			WlanClient client = new WlanClient();
			bool exit = false;
			int timeout = 500;
			int ethIndex = 0;
			int netIndex = 0;

			List<Wlan.WlanBssEntry> wlanBssEntries = new List<Wlan.WlanBssEntry>();
			ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();
			Dictionary<int, string> ethNames = new Dictionary<int, string>();

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
						Console.ForegroundColor = highlightColor;
						Console.WriteLine("\neth" + ethIndex + " \"" + ethNames[ethIndex] + "\":");
						//Console.WriteLine(wlanIface.InterfaceName + ":");
						ethIndex++;
						Console.ForegroundColor = defaultColor;

						wlanBssEntries.Clear();
						wlanBssEntries.AddRange(wlanIface.GetNetworkBssList());

						wlanBssEntries.Sort(delegate( Wlan.WlanBssEntry a, Wlan.WlanBssEntry b )
						{
							return string.Compare(ASCIIEncoding.ASCII.GetString(a.dot11Ssid.SSID),
								ASCIIEncoding.ASCII.GetString(b.dot11Ssid.SSID), StringComparison.InvariantCultureIgnoreCase);
						});

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
					if (keyPressed.Key == ConsoleKey.P || keyPressed.Key == ConsoleKey.Spacebar) {
						Console.Write("\nPAUSED: Press any key to continue");
						keyPressed = Console.ReadKey(true);
						if (keyPressed.Key == ConsoleKey.Q || keyPressed.Key == ConsoleKey.Escape) {
							exit = true;
							break;
						}
						keyPressed = new ConsoleKeyInfo();
					} else {
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

		private static void WriteNetwork( int netIndex, Wlan.WlanBssEntry network )
		{
			int mzxSsidLen = 25;
			int barLength = 25;
			string ssidName = ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).Trim('\0');
			string bar = CreateBar(network, barLength);

			if (ssidName.Length > mzxSsidLen) {
				ssidName = ssidName.Substring(0, mzxSsidLen).Trim();
			} else if (ssidName.Length == 0) {
				ssidName = "<unknown/hidden>";
			}

			Console.ForegroundColor = indexColor;
			Console.Write("{0,-3}", netIndex);
			Console.ForegroundColor = defaultColor;
			Console.Write(" {0,-" + mzxSsidLen + "}|Signal: {1,4:###} dBm |", ssidName, network.rssi);

			if (network.linkQuality > 90) {
				Console.ForegroundColor = color1;
			} else if (network.linkQuality > 80) {
				Console.ForegroundColor = color2;
			} else if (network.linkQuality > 60) {
				Console.ForegroundColor = color3;
			} else if (network.linkQuality > 30) {
				Console.ForegroundColor = color4;
			} else {
				Console.ForegroundColor = color5;
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
			// '▄' '▀' '▐' '█'
			return string.Format("{0}{1}", new string('▄', barValue), new string(' ', barLength - barValue));
		}
	}
}
