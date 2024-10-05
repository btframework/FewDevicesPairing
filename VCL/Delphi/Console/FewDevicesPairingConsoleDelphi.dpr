program FewDevicesPairingConsoleDelphi;

{$APPTYPE CONSOLE}

{$R *.res}

uses
  System.SysUtils, System.Generics.Collections, wclBluetooth, SyncObjs,
  wclMessaging, wclErrors, wclBluetoothErrors;

type
  TPairing = class
  private
    FDevices: TList<Int64>;
    FOperationEvent: TEvent;
    FOperationResult: Integer;
    FDeviceAddress: Int64;

    procedure ManagerAuthenticationCompleted(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64;
      const Error: Integer);
    procedure ManagerPinRequest(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64; out Pin: String);
    procedure ManagerPasskeyRequest(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64;
      out Passkey: Cardinal);
    procedure ManagerPasskeyNotification(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64;
      const Passkey: Cardinal);
    procedure ManagerNumericComparison(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64;
      const Number: Cardinal; out Confirm: Boolean);
    procedure ManagerDiscoveringCompleted(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Error: Integer);
    procedure ManagerDeviceFound(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64);
    procedure ManagerDiscoveringStarted(Sender: TObject;
      const Radio: TwclBluetoothRadio);

  public
    procedure Start;
  end;

procedure TPairing.ManagerAuthenticationCompleted(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; const Error: Integer);
begin
  if Address <> FDeviceAddress then begin
    Writeln('Pairing with unknown device ' + IntToHex(Address, 12) +
      ' completed with result: 0x' + IntToHex(Error, 8));

  end else begin
    if Error = WCL_E_SUCCESS then
      Writeln('Pairing with device completed with success')
    else begin
      Writeln('Pairing with device completed with error: 0x' +
        IntToHex(Error, 8));
    end;

    // Switch to next device.
    FOperationEvent.SetEvent;
  end;
end;

procedure TPairing.ManagerPinRequest(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; out Pin: String);
begin
  Pin := '';
  if Address <> FDeviceAddress then begin
    Writeln('Unknown device: ' + IntToHex(Address, 12) +
      ' requires PIN. Ignore');

  end else begin
    Writeln('Device requires requires PIN. Provdes: 0000');
    Pin := '0000';
  end;
end;

procedure TPairing.ManagerPasskeyRequest(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; out Passkey: Cardinal);
begin
  Passkey := 0;
  if Address <> FDeviceAddress then begin
    Writeln('Unknown device: ' + IntToHex(Address, 12) +
      ' requires passkey entering. Ignore');

  end else begin
    Writeln('Device requires requires passkey providing.');
    Passkey := 1234; // here you must provide the passkey.
  end;
end;

procedure TPairing.ManagerPasskeyNotification(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64;
  const Passkey: Cardinal);
begin
  if Address <> FDeviceAddress then begin
    Writeln('Unknown device: ' + IntToHex(Address, 12) +
      ' requires passkey entering. Ignore');

  end else
    Writeln('Device requires requires passkey entering.');
end;

procedure TPairing.ManagerNumericComparison(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; const Number: Cardinal;
  out Confirm: Boolean);
begin
  Confirm := False;

  if Address <> FDeviceAddress then begin
    Writeln('Unknown device: ' + IntToHex(Address, 12) +
      ' requires numeric comparison. Ignore');

  end else begin
    Writeln('Device requires numeric comparison. Accept.');
    Confirm := True;
  end;
end;

procedure TPairing.ManagerDiscoveringCompleted(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Error: Integer);
begin
  FOperationResult := Error;

  if Error = WCL_E_SUCCESS then
    Writeln('Discovering compeleted with success')
  else
    Writeln('Discovering completed with error: 0x' + IntToHex(Error, 8));

  FOperationEvent.SetEvent;
end;

procedure TPairing.ManagerDeviceFound(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64);
begin
  Writeln('Device ' + IntToHex(Address, 12) + ' found');
  FDevices.Add(Address);
end;

procedure TPairing.ManagerDiscoveringStarted(Sender: TObject;
  const Radio: TwclBluetoothRadio);
begin
  Writeln('Discovering started');
end;

procedure TPairing.Start;
var
  Manager: TwclBluetoothManager;
  Res: Integer;
  Radio: TwclBluetoothRadio;
  i: Integer;
begin
  TwclMessageBroadcaster.SetMessageProcessingMethod(mpAsync);

  Manager := TwclBluetoothManager.Create(nil);
  Manager.OnDiscoveringStarted := ManagerDiscoveringStarted;
  Manager.OnDeviceFound := ManagerDeviceFound;
  Manager.OnDiscoveringCompleted := ManagerDiscoveringCompleted;
  Manager.OnNumericComparison := ManagerNumericComparison;
  Manager.OnPasskeyNotification := ManagerPasskeyNotification;
  Manager.OnPasskeyRequest := ManagerPasskeyRequest;
  Manager.OnPinRequest := ManagerPinRequest;
  Manager.OnAuthenticationCompleted := ManagerAuthenticationCompleted;

  Res := Manager.Open;
  if Res <> WCL_E_SUCCESS then
    Writeln('Unable to open Bluetooth Manager: 0x' + IntToHex(Res, 8))

  else begin
    if Manager.Count = 0 then
      Writeln('Bluetooth hardware not found')

    else begin
      Radio := nil;

      for i := 0 to Manager.Count - 1 do begin
        if Manager[i].Available then begin
          Radio := Manager[i];
          Break;
        end;
      end;

      if Radio = nil then
        Writeln('No Bluetooth radio available')

      else begin
        Writeln('Found ' + Radio.ApiName + ' Bluetooth radio');

        FDevices := TList<Int64>.Create;
        FOperationEvent := TEvent.Create(nil, False, False, '');
        FOperationResult := WCL_E_SUCCESS;

        Writeln('Start discovering');
        Res := Radio.Discover(10, dkClassic);
        if Res <> WCL_E_SUCCESS then
          Writeln('Unable to start discovering: 0x' + IntToHex(Res, 8))

        else begin
          Writeln('Wait for discovering completion');
          FOperationEvent.WaitFor;

          if FOperationResult = WCL_E_SUCCESS then begin
            if FDevices.Count = 0 then
              Writeln('No devices were found')

            else begin
              Writeln('Starting pairing with all found devices');

              for i := 0 to FDevices.Count - 1 do begin
                FDeviceAddress := FDevices[i];
                Writeln('Start pairing with device: ' +
                  IntToHex(FDeviceAddress, 12));
                Res := Radio.RemotePair(FDeviceAddress);
                if Res = WCL_E_BLUETOOTH_ALREADY_PAIRED then begin
                  Writeln('Device already paired');
                  Continue;
                end;

                if Res <> WCL_E_SUCCESS then begin
                  Writeln('Unable to pair with device: 0x' + IntToHex(Res, 8));
                  Continue;
                end;

                Writeln('Wait for pairing result');
                FOperationEvent.WaitFor;

                // We have to wait a little bit before switching to next device.
                if i < FDevices.Count - 1 then
                  Sleep(5000);
              end;
              Writeln('Pairing completed');
            end;
          end;
        end;
        FOperationEvent.Free;
        FDevices.Free;
      end;
    end;
    Manager.Close();
  end;
  Manager.Free;
end;

begin
  try
    with TPairing.Create do begin
      Start;
      Free;
    end;

  except
    on E: Exception do
      Writeln(E.ClassName, ': ', E.Message);
  end;
end.
