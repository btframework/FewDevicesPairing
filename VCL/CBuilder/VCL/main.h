//---------------------------------------------------------------------------

#ifndef mainH
#define mainH
//---------------------------------------------------------------------------
#include <System.Classes.hpp>
#include <Vcl.Controls.hpp>
#include <Vcl.StdCtrls.hpp>
#include <Vcl.Forms.hpp>
#include "wclBluetooth.hpp"
#include <Vcl.ExtCtrls.hpp>
#include <list>
//---------------------------------------------------------------------------
class TfmMain : public TForm
{
__published:	// IDE-managed Components
	TButton *btStart;
	TButton *btStop;
	TListBox *lbLog;
	TTimer *tiWait;
	TwclBluetoothManager *Manager;
	void __fastcall btStartClick(TObject *Sender);
	void __fastcall btStopClick(TObject *Sender);
	void __fastcall FormCreate(TObject *Sender);
	void __fastcall FormDestroy(TObject *Sender);
	void __fastcall ManagerAuthenticationCompleted(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address,
		const int Error);
	void __fastcall ManagerDeviceFound(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address);
	void __fastcall ManagerDiscoveringCompleted(TObject *Sender,
		TwclBluetoothRadio * const Radio, const int Error);
	void __fastcall ManagerDiscoveringStarted(TObject *Sender,
		TwclBluetoothRadio * const Radio);
	void __fastcall ManagerNumericComparison(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address,
		const DWORD Number, bool& Confirm);
	void __fastcall ManagerPasskeyNotification(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address,
		const DWORD Passkey);
	void __fastcall ManagerPasskeyRequest(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address,
		DWORD& Passkey);
	void __fastcall ManagerPinRequest(TObject *Sender,
		TwclBluetoothRadio * const Radio, const __int64 Address,
		UnicodeString& Pin);
	void __fastcall tiWaitTimer(TObject *Sender);

private:	// User declarations
	TwclBluetoothRadio* FRadio;
	std::list<__int64> FDevices;
	size_t FCurrentDeviceIndex;
	__int64 FAddress;

	void __fastcall Stop();
	void __fastcall Start();
	void __fastcall PairWithNextDevice();

public:		// User declarations
	__fastcall TfmMain(TComponent* Owner);
};
//---------------------------------------------------------------------------
extern PACKAGE TfmMain *fmMain;
//---------------------------------------------------------------------------
#endif
