//-----------------------------------------
//   Remote Control Server (RCS)
//   Remote Control Client (RCC)
//           release 4
//-----------------------------------------

本ソフトは、予めリモート側で設定しておいたアプリやコマンドを、
クライアント側から遠隔で「実行」させられるソフトです。

例えば、リモートPCで「RCS」の「commands.ini」の1行目に「mspaint」と記載・保存してRCSを実行しておき、IPアドレスを192.168.100.102とし、RCSを実行しておきます。
次に、クライアントPCで「RCC」の「settings.ini」に「192.168.100.102,2005,1」と記載・保存してRCCを実行すると、
リモートPC側でペイントを起動させることができます。

使用前に、RCCのsettings.iniとRCSのcommands.iniを、任意に書き換えてください。




-------------------------
DD-HOST
http://www.ddhost.jp