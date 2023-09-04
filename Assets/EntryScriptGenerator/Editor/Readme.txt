◆EntryScriptSettings

指定したフォルダにInterfaceとclassを自動生成するツールです。

使用方法：
Unityメニュー：Window → CBank → EntryScriptSettingsから起動 
①Prefixに生成するファイル名のPrefixを設定
②「Interface Paths」にInterfaceを生成するフォルダパスを指定（複数可）
③「Interface Suffix Names」に生成するファイルのSuffixを設定
例）
Prefix: Test
Interface Suffix Names：UseCase
生成ファイル名：ITestUseCase

④「Class Paths」にClassを生成するフォルダパスを指定（複数可）
⑤「Class Suffix Names」に生成するファイルのSuffixを設定
例）
Prefix: Test
Interface Suffix Names：UseCase
生成ファイル名：TestUseCase

⑥「Class Interface Indexes」に②で設定した「Interface Paths」との紐付けを設定
-1は無効値、0以上のIndexで設定してください

⑦「出力する」を実行するとファイルが出力されます。
