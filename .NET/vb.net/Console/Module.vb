Module Module1
    Private FDevices As List(Of Int64)
    Private FOperationEvent As AutoResetEvent
    Private FOperationResult As Int32
    Private FAddress As Int64

    Private WithEvents Manager As wclBluetoothManager = New wclBluetoothManager()

    Sub Main()
        Manager.MessageProcessing = wclMessageProcessingMethod.mpAsync

        Dim Res As Int32 = Manager.Open()
        If Res <> wclErrors.WCL_E_SUCCESS Then
            Console.WriteLine("Unable to open Bluetooth Manager: 0x" + Res.ToString("X8"))
        Else
            If Manager.Count = 0 Then
                Console.WriteLine("Bluetooth hardware not found")
            Else
                Dim Radio As wclBluetoothRadio = Nothing

                For i As Integer = 0 To Manager.Count - 1
                    If Manager(i).Available Then
                        Radio = Manager(i)
                        Exit For
                    End If
                Next i

                If Radio Is Nothing Then
                    Console.WriteLine("No Bluetooth radio available")
                Else
                    Console.WriteLine("Found " + Radio.ApiName + " Bluetooth radio")

                    FDevices = New List(Of Int64)()
                    FOperationEvent = New AutoResetEvent(False)
                    FOperationResult = wclErrors.WCL_E_SUCCESS

                    Console.WriteLine("Start discovering")
                    Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic)
                    If Res <> wclErrors.WCL_E_SUCCESS Then
                        Console.WriteLine("Unable to start discovering: 0x" + Res.ToString("X8"))
                    Else
                        Console.WriteLine("Wait for discovering completion")
                        FOperationEvent.WaitOne()

                        If FOperationResult = wclErrors.WCL_E_SUCCESS Then
                            If FDevices.Count = 0 Then
                                Console.WriteLine("No devices were found")
                            Else
                                Console.WriteLine("Starting pairing with all found devices")

                                For i As Integer = 0 To FDevices.Count - 1
                                    FAddress = FDevices(i)
                                    Console.WriteLine("Start pairing with device: " + FAddress.ToString("X12"))
                                    Res = Radio.RemotePair(FAddress)
                                    If Res = wclBluetoothErrors.WCL_E_BLUETOOTH_ALREADY_PAIRED Then
                                        Console.WriteLine("Device already paired")
                                        Continue For
                                    End If

                                    If Res <> wclErrors.WCL_E_SUCCESS Then
                                        Console.WriteLine("Unable to pair with device: 0x" + Res.ToString("X8"))
                                        Continue For
                                    End If

                                    Console.WriteLine("Wait for pairing result")
                                    FOperationEvent.WaitOne()

                                    ' We have to wait a little bit before switching to next device.
                                    If i < FDevices.Count - 1 Then Thread.Sleep(5000)
                                Next i

                                Console.WriteLine("Pairing completed")
                            End If
                        End If
                    End If

                    FOperationEvent.Close()
                    FOperationEvent = Nothing

                    FDevices.Clear()
                    FDevices = Nothing

                    Radio = Nothing
                End If
            End If

            Manager.Close()
        End If
        Manager = Nothing
    End Sub

    Private Sub Manager_OnDiscoveringStarted(Sender As Object, Radio As wclBluetoothRadio) Handles Manager.OnDiscoveringStarted
        Console.WriteLine("Discovering started")
    End Sub

    Private Sub Manager_OnDeviceFound(Sender As Object, Radio As wclBluetoothRadio, Address As Long) Handles Manager.OnDeviceFound
        Console.WriteLine("Device " + Address.ToString("X12") + " found")
        FDevices.Add(Address)
    End Sub

    Private Sub Manager_OnDiscoveringCompleted(Sender As Object, Radio As wclBluetoothRadio, [Error] As Integer) Handles Manager.OnDiscoveringCompleted
        FOperationResult = [Error]

        If [Error] = wclErrors.WCL_E_SUCCESS Then
            Console.WriteLine("Discovering compeleted with success")
        Else
            Console.WriteLine("Discovering completed with error: 0x" + [Error].ToString("X8"))
        End If

        FOperationEvent.Set()
    End Sub

    Private Sub Manager_OnNumericComparison(Sender As Object, Radio As wclBluetoothRadio, Address As Long, Number As UInteger, ByRef Confirm As Boolean) Handles Manager.OnNumericComparison
        Confirm = False

        If Address <> FAddress Then
            Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires numeric comparison. Ignore")
        Else
            Console.WriteLine("Device requires numeric comparison. Accept.")
            Confirm = True
        End If
    End Sub

    Private Sub Manager_OnPasskeyNotification(Sender As Object, Radio As wclBluetoothRadio, Address As Long, Passkey As UInteger) Handles Manager.OnPasskeyNotification
        If Address <> FAddress Then
            Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore")
        Else
            Console.WriteLine("Device requires requires passkey entering.")
        End If
    End Sub

    Private Sub Manager_OnPasskeyRequest(Sender As Object, Radio As wclBluetoothRadio, Address As Long, ByRef Passkey As UInteger) Handles Manager.OnPasskeyRequest
        Passkey = 0
        If Address <> FAddress Then
            Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore")
        Else
            Console.WriteLine("Device requires requires passkey providing.")
            Passkey = 1234 ' here you must provide the passkey.
        End If
    End Sub

    Private Sub Manager_OnPinRequest(Sender As Object, Radio As wclBluetoothRadio, Address As Long, ByRef Pin As String) Handles Manager.OnPinRequest
        Pin = ""
        If Address <> FAddress Then
            Console.WriteLine("Unknown device: " + Address.ToString("X12") + " requires PIN. Ignore")
        Else
            Console.WriteLine("Device requires requires PIN. Provdes: 0000")
            Pin = "0000"
        End If
    End Sub

    Private Sub Manager_OnAuthenticationCompleted(Sender As Object, Radio As wclBluetoothRadio, Address As Long, [Error] As Integer) Handles Manager.OnAuthenticationCompleted
        If Address <> FAddress Then
            Console.WriteLine("Pairing with unknown device " + Address.ToString("X12") + " completed with result: 0x" + [Error].ToString("X8"))
        Else
            If [Error] = wclErrors.WCL_E_SUCCESS Then
                Console.WriteLine("Pairing with device completed with success")
            Else
                Console.WriteLine("Pairing with device completed with error: 0x" + [Error].ToString("X8"))

                ' Switch to next device.
                FOperationEvent.Set()
            End If
        End If
    End Sub
End Module


