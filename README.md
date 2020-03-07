# GoogleDynamicDNSUpdater
This windows service will update the Google Domains Dynamic DNS service with your external IP address.

Please read and follow these steps to setup and install the service on your Windows computer:

1) Download the solution to your local machine.
2) Open the solution using Visual Studio.
3) From the solution explorer open the App.config file.
4) Update the 'Google Domains Credentials' values.
5) Set the solution configuration to "Release" and build the solution (note you can also build in Debug if you prefer).
6) Open Command Prompt as an Administrator and go to the executables directory: 
</br> ...\GoogleDynamicDNSUpdater-master\GoogleDynamicDNS\bin\Release
7) Type the following to install the service (note the location of IntallUtil.exe may change in the future):
</br> C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe "GoogleDynamicDNS.exe"
8) In the Set Service Login window enter the Windows User credentials for the service.
9) Open Services and navigate to the service "GoogleDynamicDNSUpdater". 
10) Right click it and select "Start".
11) Back in the executables directory you should now see a file named Log_[Date].txt which you should review to confirm success.

Other things worth noting:
1) To Uninstall:
  </br> a) Open Command Prompt as an Administrator and go to the executables directory: 
  </br>   ...\GoogleDynamicDNSUpdater-master\GoogleDynamicDNS\bin\Release
  </br> b) Type the following to remove the service (note the location of IntallUtil.exe may change in the future):
  </br>   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u "GoogleDynamicDNS.exe"

2) The service is automatically set to start with windows. This can be changed in the Services (Properties -> Start Up Type -> Manual)

3) This can be service can be run from Visual Studio directly if you do not want to install it.
  Note: Make sure solution is in debug or you will receive an error when you try to start.

4) This application makes a web request to "http://checkip.dyndns.org/" to get the external IP address.

5) Google Domains Dynamic DNS API documentation can be found here:
  https://support.google.com/domains/answer/6147083?hl=en

