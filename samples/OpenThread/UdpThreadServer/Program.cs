﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Net;
using System.Threading;
using nanoFramework.Networking.Thread;

namespace Samples
{
    public class Program
    {
        private const int UDP_PORT = 1234;

        private static OpenThread _ot;
        private static AutoResetEvent _waitNetAttached = new AutoResetEvent(false);
        
        public static Led _led = new Led();

        public static void Main()
        {
            Console.WriteLine();
            Display.Log("Sample UDP thread UDP server");

            Display.LogMemoryStats("Start up");

            _led.Set(ThreadDeviceRole.Disabled);

            // Init OpenThread stack
            InitThread();

            Display.Log("Wait for OpenThread to be attached...");
            _waitNetAttached.WaitOne();

            Display.Log("== Demonstrate some CLI commands");
            Display.Log("- Display current active dataset");
            CommandAndResult("dataset active");

            Display.Log("Display interface IP addresses");
            CommandAndResult("ipaddr");

            IPAddress adr = _ot.MeshLocalAddress;
            Display.Log($"Local Mesh address {adr}");

            Display.Log("Open UDP socket for communication");
            NetUtils.OpenUdpSocket("", UDP_PORT, IPAddress.IPv6Any);

            Display.Log("Start a receive thread to respond to UDP messages");
            Thread ReceiveUdpThread = new Thread(() => NetUtils.ReceiveUdpMessages(true));
            ReceiveUdpThread.Start();

            Thread.Sleep(Timeout.Infinite);
        }

        static void CommandAndResult(string cmd)
        {
            Console.WriteLine($"{Display.LH} command>{cmd}");
            string[] results = _ot.CommandLineInputAndWaitResponse(cmd);
            Display.Log(results);
        }

        #region OpenThread

        /// <summary>
        /// Initialize the OpenThread
        /// </summary>
        static void InitThread()
        {
            OpenThreadDataset data = new OpenThreadDataset()
            {
                // Minimum data required to set up/connect to Thread network
                NetworkName = "nanoFramework",
                // 000102030405060708090A0B0C0D0E0F
                NetworkKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
                PanId = 0x1234,
                Channel = 15
            };

            Display.Log("---- Thread Dataset ------");
            Display.Log($"Network name {data.NetworkName}");
            Display.Log($"NetworkKey   {BitConverter.ToString(data.NetworkKey)}");
            Display.Log($"Channel      {data.Channel}");
            Display.Log("---- Thread Dataset end ------");

            // Use local radio, ESP32_C6 or ESP32_H2
            _ot = OpenThread.CreateThreadWithNativeRadio(ThreadDeviceType.Router);

            // Set up event handlers
            _ot.OnStatusChanged += Ot_OnStatusChanged;
            _ot.OnRoleChanged += Ot_OnRoleChanged;
            _ot.OnConsoleOutputAvailable += Ot_OnConsoleOutputAvailable;

            _ot.Dataset = data;

            Display.Log($"Starting OpenThread stack");
            _ot.Start();
        }

        private static void Ot_OnRoleChanged(OpenThread sender, OpenThreadRoleChangeEventArgs args)
        {
            Display.Role(args.currentRole);
            _led.Set(args.currentRole);
        }

        private static void Ot_OnStatusChanged(OpenThread sender, OpenThreadStateChangeEventArgs args)
        {
            switch ((ThreadDeviceState)args.currentState)
            {
                case ThreadDeviceState.Detached:
                    Display.Log("Status - Detached");
                    _led.Set(ThreadDeviceRole.Disabled);
                    break;

                case ThreadDeviceState.Attached:
                    Display.Log("Status - Attached");
                    _waitNetAttached.Set();
                    break;

                case ThreadDeviceState.GotIpv6:
                    Display.Log("Status - Got IPV6 address");
                    break;

                case ThreadDeviceState.Start:
                    Display.Log("Status - Started");
                    break;

                case ThreadDeviceState.Stop:
                    Display.Log("Status - Stopped");
                    break;

                case ThreadDeviceState.InterfaceUp:
                    Display.Log("Status - Interface UP");
                    break;

                case ThreadDeviceState.InterfaceDown:
                    Display.Log("Status - Interface DOWN");
                    break;

                default:
                    Display.Log($"Status - changed to {args.currentState}");
                    break;
            }
        }
        private static void Ot_OnConsoleOutputAvailable(OpenThread sender, OpenThreadConsoleOutputAvailableArgs args)
        {
            Display.Log(args.consoleLines);
        }

        #endregion
    }
}
