using System;
using System.Threading;
using System.Collections.Generic;

using wclCommon;
using wclBluetooth;

namespace FewDevicesPairingConsole
{
    class Program
    {
        private static List<Int64> FDevices;
        private static AutoResetEvent FOperationEvent;
        private static Int32 FOperationResult;
        private static Int64 FAddress;

        static void Main(string[] args)
        {
            wclBluetoothManager Manager = new wclBluetoothManager();
            Manager.MessageProcessing = wclMessageProcessingMethod.mpAsync;
            Manager.OnDiscoveringStarted += new wclBluetoothEvent(Manager_OnDiscoveringStarted);
            Manager.OnDeviceFound += new wclBluetoothDeviceEvent(Manager_OnDeviceFound);
            Manager.OnDiscoveringCompleted += new wclBluetoothResultEvent(Manager_OnDiscoveringCompleted);
            Manager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
            Manager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
            Manager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
            Manager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);
            Manager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(Manager_OnAuthenticationCompleted);

            Int32 Res = Manager.Open();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("Unable to open Bluetooth Manager: 0x" + Res.ToString("X8"));
            else
            {
                if (Manager.Count == 0)
                    Console.WriteLine("Bluetooth hardware not found");
                else
                {
                    wclBluetoothRadio Radio = null;

                    for (int i = 0; i < Manager.Count; i++)
                    {
                        if (Manager[i].Available)
                        {
                            Radio = Manager[i];
                            break;
                        }
                    }

                    if (Radio == null)
                        Console.WriteLine("No Bluetooth radio available");
                    else
                    {
                        Console.WriteLine("Found " + Radio.ApiName + " Bluetooth radio");

                        FDevices = new List<Int64>();
                        FOperationEvent = new AutoResetEvent(false);
                        FOperationResult = wclErrors.WCL_E_SUCCESS;

                        Console.WriteLine("Start discovering");
                        Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            Console.WriteLine("Unable to start discovering: 0x" + Res.ToString("X8"));
                        else
                        {
                            Console.WriteLine("Wait for discovering completion");
                            FOperationEvent.WaitOne();

                            if (FOperationResult == wclErrors.WCL_E_SUCCESS)
                            {
                                if (FDevices.Count == 0)
                                    Console.WriteLine("No devices were found");
                                else
                                {
                                    Console.WriteLine("Starting pairing with all found devices");

                                    for (int i = 0; i < FDevices.Count; i++)
                                    {
                                        FAddress = FDevices[i];
                                        Console.WriteLine("Start pairing with device: " + FAddress.ToString("X12"));
                                        Res = Radio.RemotePair(FAddress);
                                        if (Res == wclBluetoothErrors.WCL_E_BLUETOOTH_ALREADY_PAIRED)
                                        {
                                            Console.WriteLine("Device already paired");
                                            continue;
                                        }

                                        if (Res != wclErrors.WCL_E_SUCCESS)
                                        {
                                            Console.WriteLine("Unable to pair with device: 0x" + Res.ToString("X8"));
                                            continue;
                                        }

                                        Console.WriteLine("Wait for pairing result");
                                        FOperationEvent.WaitOne();

                                        // We have to wait a little bit before switching to next device.
                                        if (i < FDevices.Count - 1)
                                            Thread.Sleep(5000);
                                    }

                                    Console.WriteLine("Pairing completed");
                                }
                            }
                        }

                        FOperationEvent.Close();
                        FOperationEvent = null;

                        FDevices.Clear();
                        FDevices = null;

                        Radio = null;
                    }
                }

                Manager.Close();
            }

            Manager = null;
        }

        private static void Manager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, long Address, int Error)
        {
            if (Address != FAddress)
                Console.WriteLine("Pairing with unknown device " + Address.ToString("X12") + " completed with result: 0x" + Error.ToString("X8"));
            else
            {
                if (Error == wclErrors.WCL_E_SUCCESS)
                    Console.WriteLine("Pairing with device completed with success");
                else
                    Console.WriteLine("Pairing with device completed with error: 0x" + Error.ToString("X8"));

                // Switch to next device.
                FOperationEvent.Set();
            }
        }

        private static void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
        {
            Pin = "";
            if (Address != FAddress)
                Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires PIN. Ignore");
            else
            {
                Console.WriteLine("Device requires requires PIN. Provdes: 0000");
                Pin = "0000";
            }
        }

        private static void Manager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
        {
            Passkey = 0;
            if (Address != FAddress)
                Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
            {
                Console.WriteLine("Device requires requires passkey providing.");
                Passkey = 1234; // here you must provide the passkey.
            }
        }

        private static void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
        {
            if (Address != FAddress)
                Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore");
            else
                Console.WriteLine("Device requires requires passkey entering.");
        }

        private static void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
        {
            Confirm = false;

            if (Address != FAddress)
                Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires numeric comparison. Ignore");
            else
            {
                Console.WriteLine("Device requires numeric comparison. Accept.");
                Confirm = true;
            }
        }
        
        private static void  Manager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, int Error)
        {
            FOperationResult = Error;

            if (Error == wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("Discovering compeleted with success");
            else
                Console.WriteLine("Discovering completed with error: 0x" + Error.ToString("X8"));
            
            FOperationEvent.Set();
        }
        
        private static void  Manager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, long Address)
        {
            Console.WriteLine("Device " + Address.ToString("X12") + " found");
            FDevices.Add(Address);
        }

        private static void Manager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
        {
            Console.WriteLine("Discovering started");
        }
    }
}
