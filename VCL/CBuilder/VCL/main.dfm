object fmMain: TfmMain
  Left = 0
  Top = 0
  BorderStyle = bsSingle
  Caption = 'Few devices pairing demo application'
  ClientHeight = 409
  ClientWidth = 645
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  OldCreateOrder = False
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  PixelsPerInch = 96
  TextHeight = 13
  object btStart: TButton
    Left = 8
    Top = 8
    Width = 75
    Height = 25
    Caption = 'Start'
    TabOrder = 0
    OnClick = btStartClick
  end
  object btStop: TButton
    Left = 89
    Top = 8
    Width = 75
    Height = 25
    Caption = 'Stop'
    TabOrder = 1
    OnClick = btStopClick
  end
  object lbLog: TListBox
    Left = 8
    Top = 39
    Width = 629
    Height = 362
    ItemHeight = 13
    TabOrder = 2
  end
  object tiWait: TTimer
    Enabled = False
    Interval = 5000
    OnTimer = tiWaitTimer
    Left = 328
    Top = 256
  end
  object Manager: TwclBluetoothManager
    OnAuthenticationCompleted = ManagerAuthenticationCompleted
    OnDeviceFound = ManagerDeviceFound
    OnDiscoveringCompleted = ManagerDiscoveringCompleted
    OnDiscoveringStarted = ManagerDiscoveringStarted
    OnNumericComparison = ManagerNumericComparison
    OnPasskeyNotification = ManagerPasskeyNotification
    OnPasskeyRequest = ManagerPasskeyRequest
    OnPinRequest = ManagerPinRequest
    Left = 184
    Top = 288
  end
end
