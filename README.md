# IWasHere
fluxcess Check-In Software "I Was Here"

More information: https://fluxcess.com


### Developer information

Solution used for editing: Microsoft Visual Studio Community Edition 2015

#### Signing the exe file after building:

~~~

signtool.exe sign /p <password> /t http://timestamp.comodoca.com /f <cert pfx file> /v "<path to IWasHere.exe>"

signtool.exe sign /p <password> /f <cert pfx file> /fd sha256 /tr http://timestamp.comodoca.com/?td=sha256 /td sha256 /as /v "<path to IWasHere.exe>"

~~~