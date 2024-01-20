unit main;

interface

uses
  Vcl.Forms, wclBluetooth, Vcl.ExtCtrls, Vcl.Controls, Vcl.StdCtrls,
  System.Classes, System.Generics.Collections;

type
  TfmMain = class(TForm)
    btStart: TButton;
    btStop: TButton;
    lbLog: TListBox;
    tiWait: TTimer;
    Manager: TwclBluetoothManager;
    procedure FormCreate(Sender: TObject);
    procedure ManagerAuthenticationCompleted(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64;
      const Error: Integer);
    procedure ManagerPinRequest(Sender: TObject;
      const Radio: TwclBluetoothRadio; const Address: Int64; out Pin: string);
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
    procedure btStopClick(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure btStartClick(Sender: TObject);
    procedure tiWaitTimer(Sender: TObject);

  private
    FRadio: TwclBluetoothRadio;
    FDevices: TList<Int64>;
    FCurrentDeviceIndex: Integer;
    FAddress: Int64;

    procedure Stop;
    procedure Start;
    procedure PairWithNextDevice;
  end;

var
  fmMain: TfmMain;

implementation

{$R *.dfm}

uses
  wclErrors, SysUtils, wclBluetoothErrors;

{ TfmMain }

procedure TfmMain.btStartClick(Sender: TObject);
begin
  if FRadio <> nil then
    lbLog.Items.Add('Pairing running')
  else
    Start;
end;

procedure TfmMain.btStopClick(Sender: TObject);
begin
  if FRadio = nil then
    lbLog.Items.Add('Pairing not running')
  else
    Stop;
end;

procedure TfmMain.FormCreate(Sender: TObject);
begin
  FDevices := TList<Int64>.Create;
  FRadio := nil;
end;

procedure TfmMain.FormDestroy(Sender: TObject);
begin
  Stop;
  FDevices.Free;
end;

procedure TfmMain.ManagerAuthenticationCompleted(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; const Error: Integer);
begin
  if Address <> FAddress then begin
    lbLog.Items.Add('Pairing with unknown device ' + IntToHex(Address, 12) +
      ' completed with result: 0x' + IntToHex(Error, 8));

  end else begin
    if Error = WCL_E_SUCCESS then
      lbLog.Items.Add('Pairing with device completed with success')
    else begin
      lbLog.Items.Add('Pairing with device completed with error: 0x' +
        IntToHex(Error, 8));
    end;

    if Radio <> nil then begin
      lbLog.Items.Add('Switching to next device');
      tiWait.Enabled := True;
    end;
  end;
end;

procedure TfmMain.ManagerDeviceFound(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64);
begin
  lbLog.Items.Add('Device ' + IntToHex(Address, 12) + ' found');
  FDevices.Add(Address);
end;

procedure TfmMain.ManagerDiscoveringCompleted(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Error: Integer);
begin
  if Error <> WCL_E_SUCCESS then begin
    lbLog.Items.Add('Discovering completed with error: 0x' +
      IntToHex(Error, 8));
    Stop;

  end else begin
    lbLog.Items.Add('Discovering comepleted');
    if Radio <> nil then begin
      if FDevices.Count = 0 then begin
        lbLog.Items.Add('No Bluetooth devices were found');
        Stop;

      end else begin
        lbLog.Items.Add('Starting pairing');
        PairWithNextDevice;
      end;
    end;
  end;
end;

procedure TfmMain.ManagerDiscoveringStarted(Sender: TObject;
  const Radio: TwclBluetoothRadio);
begin
  FDevices.Clear;
  FCurrentDeviceIndex := 0;

  lbLog.Items.Add('Discovering started');
end;

procedure TfmMain.ManagerNumericComparison(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; const Number: Cardinal;
  out Confirm: Boolean);
begin
  Confirm := False;

  if Address <> FAddress then begin
    lbLog.Items.Add('Unknown device: ' + IntToHex(Address, 12) +
      ' requires numeric comparison. Ignore');

  end else begin
    lbLog.Items.Add('Device requires numeric comparison. Accept.');
    Confirm := True;
  end;
end;

procedure TfmMain.ManagerPasskeyNotification(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64;
  const Passkey: Cardinal);
begin
  if Address <> FAddress then begin
    lbLog.Items.Add('Unknown device: ' + IntToHex(Address, 12) +
      ' requires passkey entering. Ignore');

  end else
    lbLog.Items.Add('Device requires requires passkey entering.');
end;

procedure TfmMain.ManagerPasskeyRequest(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; out Passkey: Cardinal);
begin
  Passkey := 0;
  if Address <> FAddress then begin
    lbLog.Items.Add('Unknown device: ' + IntToHex(Address, 12) +
      ' requires passkey entering. Ignore');

  end else begin
    lbLog.Items.Add('Device requires requires passkey providing.');
    Passkey := 1234; // here you must provide the passkey.
  end;
end;

procedure TfmMain.ManagerPinRequest(Sender: TObject;
  const Radio: TwclBluetoothRadio; const Address: Int64; out Pin: string);
begin
  Pin := '';
  if Address <> FAddress then begin
    lbLog.Items.Add('Unknown device: ' + IntToHex(Address, 12) +
      ' requires PIN. Ignore');

  end else begin
    lbLog.Items.Add('Device requires requires PIN. Provdes: 0000');
    Pin := '0000';
  end;
end;

procedure TfmMain.PairWithNextDevice;
var
  Res: Integer;
begin
  tiWait.Enabled := False;

  while True do begin
    FAddress := FDevices[FCurrentDeviceIndex];
    lbLog.Items.Add('Try to pair with device: ' + IntToHex(FAddress, 12));

    Res := FRadio.RemotePair(FAddress);
    if Res <> WCL_E_SUCCESS then
      Break;
    if Res = WCL_E_BLUETOOTH_ALREADY_PAIRED then
      lbLog.Items.Add('Device is already paired')
    else begin
      lbLog.Items.Add('Start pairing with device failed; 0x' +
        IntToHex(Res, 8));
    end;

    Inc(FCurrentDeviceIndex);
    if FCurrentDeviceIndex = FDevices.Count then begin
      lbLog.Items.Add('No more devices');
      Stop;
      Break;
    end;
  end;
end;

procedure TfmMain.Start;
var
  Res: Integer;
  i: Integer;
begin
  lbLog.Items.Clear;

  Res := Manager.Open;
  if Res <> WCL_E_SUCCESS then
    lbLog.Items.Add('Unable to open Bluetooth Manager: 0x' + IntToHex(Res, 8))

  else begin
    if Manager.Count = 0 then
      lbLog.Items.Add('Bluetooth hardware not found')

    else begin
      for i := 0 to Manager.Count - 1 do begin
        if Manager[i].Available then begin
          FRadio := Manager[i];
          Break;
        end;
      end;

      if FRadio = nil then
        lbLog.Items.Add('No available Bluetooth radio found')

      else begin
        lbLog.Items.Add('Try to start discovering');
        Res := FRadio.Discover(10, dkClassic);
        if Res <> WCL_E_SUCCESS then begin
          lbLog.Items.Add('Failed to start discovering: 0x' + IntToHex(Res, 8));
          FRadio := nil;
        end;
      end;
    end;
    // If something went wrong we must close manager.
    if FRadio = nil then
      Manager.Close;
  end;
end;

procedure TfmMain.Stop;
begin
  // Cleanup Radio object.
  FRadio := nil;
  // Stop timer.
  tiWait.Enabled := False;
  // Close Bluetooth Manager.
  Manager.Close;
  // Clear found devices list.
  FDevices.Clear;
end;

procedure TfmMain.tiWaitTimer(Sender: TObject);
begin
  tiWait.Enabled := False;

  if FRadio <> nil then begin
    Inc(FCurrentDeviceIndex);
    if FCurrentDeviceIndex = FDevices.Count then begin
      lbLog.Items.Add('No more devices');
      Stop;

    end else
      PairWithNextDevice;
  end;
end;

end.
