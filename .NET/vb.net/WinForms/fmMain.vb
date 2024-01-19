Public Class fmMain
    Private WithEvents Manager As wclBluetoothManager
    Private Radio As wclBluetoothRadio
    Private Devices As List(Of Int64)
    Private CurrentDeviceIndex As Integer
    Private Address As Int64

    Private Sub [Stop]()
        ' Cleanup Radio object.
        Radio = Nothing
        ' Stop timer.
        tiWait.Stop()
        ' Close Bluetooth Manager.
        Manager.Close()
        ' Clear found devices list.
        Devices.Clear()
    End Sub

    Private Sub Start()
        lbLog.Items.Clear()

        Dim Res As Int32 = Manager.Open()
        If Res <> wclErrors.WCL_E_SUCCESS Then
            lbLog.Items.Add("Unable to open Bluetooth Manager: 0x" + Res.ToString("X8"))
        Else
            If Manager.Count = 0 Then
                lbLog.Items.Add("Bluetooth hardware not found")
            Else
                For i As Integer = 0 To Manager.Count - 1
                    If Manager(i).Available Then
                        Radio = Manager(i)
                        Exit For
                    End If
                Next i

                If Radio Is Nothing Then
                    lbLog.Items.Add("No available Bluetooth radio found")
                Else
                    lbLog.Items.Add("Try to start discovering")
                    Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic)
                    If Res <> wclErrors.WCL_E_SUCCESS Then
                        lbLog.Items.Add("Failed to start discovering: 0x" + Res.ToString("X8"))
                        Radio = Nothing
                    End If
                End If
            End If

            ' If something went wrong we must close manager.
            If Radio Is Nothing Then Manager.Close()
        End If
    End Sub

    Private Sub PairWithNextDevice()
        tiWait.Stop()

        While True
            Address = Devices(CurrentDeviceIndex)
            lbLog.Items.Add("Try to pair with device: " + Address.ToString("X12"))

            Dim Res As Int32 = Radio.RemotePair(Address)
            If Res = wclErrors.WCL_E_SUCCESS Then Exit While
            If Res = wclBluetoothErrors.WCL_E_BLUETOOTH_ALREADY_PAIRED Then
                lbLog.Items.Add("Device is already paired")
            Else
                lbLog.Items.Add("Start pairing with device failed; 0x" + Res.ToString("X8"))
            End If

            CurrentDeviceIndex = CurrentDeviceIndex + 1
            If CurrentDeviceIndex = Devices.Count Then
                lbLog.Items.Add("No more devices")
                [Stop]()
                Exit While
            End If
        End While
    End Sub

    Private Sub fmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Manager = New wclBluetoothManager()

        Devices = New List(Of Int64)()
        Radio = Nothing
    End Sub

    Private Sub Manager_OnAuthenticationCompleted(Sender As Object, Radio As wclBluetoothRadio, Address As Long, [Error] As Integer) Handles Manager.OnAuthenticationCompleted
        If Address <> Me.Address Then
            lbLog.Items.Add("Pairing with unknown device " + Address.ToString("X12") + " completed with result: 0x" + [Error].ToString("X8"))
        Else
            If [Error] = wclErrors.WCL_E_SUCCESS Then
                lbLog.Items.Add("Pairing with device completed with success")
            Else
                lbLog.Items.Add("Pairing with device completed with error: 0x" + [Error].ToString("X8"))
            End If

            If Radio IsNot Nothing Then
                lbLog.Items.Add("Switching to next device")
                tiWait.Start()
            End If
        End If
    End Sub

    Private Sub Manager_OnPinRequest(Sender As Object, Radio As wclBluetoothRadio, Address As Long, ByRef Pin As String) Handles Manager.OnPinRequest
        Pin = ""
        If Address <> Me.Address Then
            lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires PIN. Ignore")
        Else
            lbLog.Items.Add("Device requires requires PIN. Provdes: 0000")
            Pin = "0000"
        End If
    End Sub

    Private Sub Manager_OnPasskeyRequest(Sender As Object, Radio As wclBluetoothRadio, Address As Long, ByRef Passkey As UInteger) Handles Manager.OnPasskeyRequest
        Passkey = 0
        If Address <> Me.Address Then
            lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore")
        Else
            lbLog.Items.Add("Device requires requires passkey providing.")
            Passkey = 1234 ' here you must provide the passkey.
        End If
    End Sub

    Private Sub Manager_OnPasskeyNotification(Sender As Object, Radio As wclBluetoothRadio, Address As Long, Passkey As UInteger) Handles Manager.OnPasskeyNotification
        If Address <> Me.Address Then
            lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires passkey entering. Ignore")
        Else
            lbLog.Items.Add("Device requires requires passkey entering.")
        End If
    End Sub

    Private Sub Manager_OnNumericComparison(Sender As Object, Radio As wclBluetoothRadio, Address As Long, Number As UInteger, ByRef Confirm As Boolean) Handles Manager.OnNumericComparison
        Confirm = False

        If Address <> Me.Address Then
            lbLog.Items.Add("Unknown device: " + Address.ToString("X12") + " requires numeric comparison. Ignore")
        Else
            lbLog.Items.Add("Device requires numeric comparison. Accept.")
            Confirm = True
        End If
    End Sub

    Private Sub Manager_OnDiscoveringCompleted(Sender As Object, Radio As wclBluetoothRadio, [Error] As Integer) Handles Manager.OnDiscoveringCompleted
        If [Error] <> wclErrors.WCL_E_SUCCESS Then
            lbLog.Items.Add("Discovering completed with error: 0x" + [Error].ToString("X8"))
            [Stop]()
        Else
            lbLog.Items.Add("Discovering comepleted")
            If Radio IsNot Nothing Then
                If Devices.Count = 0 Then
                    lbLog.Items.Add("No Bluetooth devices were found")
                    [Stop]()
                Else
                    lbLog.Items.Add("Starting pairing")
                    PairWithNextDevice()
                End If
            End If
        End If
    End Sub

    Private Sub Manager_OnDeviceFound(Sender As Object, Radio As wclBluetoothRadio, Address As Long) Handles Manager.OnDeviceFound
        lbLog.Items.Add("Device " + Address.ToString("X12") + " found")
        Devices.Add(Address)
    End Sub

    Private Sub Manager_OnDiscoveringStarted(Sender As Object, Radio As wclBluetoothRadio) Handles Manager.OnDiscoveringStarted
        Devices.Clear()
        CurrentDeviceIndex = 0

        lbLog.Items.Add("Discovering started")
    End Sub

    Private Sub btStop_Click(sender As Object, e As EventArgs) Handles btStop.Click
        If Radio Is Nothing Then
            lbLog.Items.Add("Pairing not running")
        Else
            [Stop]()
        End If
    End Sub

    Private Sub fmMain_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        [Stop]()
        Manager = Nothing
        Devices = Nothing
    End Sub

    Private Sub btStart_Click(sender As Object, e As EventArgs) Handles btStart.Click
        If Radio IsNot Nothing Then
            lbLog.Items.Add("Pairing running")
        Else
            Start()
        End If
    End Sub

    Private Sub tiWait_Tick(sender As Object, e As EventArgs) Handles tiWait.Tick
        tiWait.Stop()

        If Radio IsNot Nothing Then
            CurrentDeviceIndex = CurrentDeviceIndex + 1
            If CurrentDeviceIndex = Devices.Count Then
                lbLog.Items.Add("No more devices")
                [Stop]()
            Else
                PairWithNextDevice()
            End If
        End If
    End Sub
End Class
