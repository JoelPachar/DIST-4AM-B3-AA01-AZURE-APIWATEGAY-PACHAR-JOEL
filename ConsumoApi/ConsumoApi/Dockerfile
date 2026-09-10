FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022
WORKDIR /inetpub/wwwroot
RUN powershell -NoProfile -Command Remove-Item -Recurse C:\inetpub\wwwroot\*
COPY . .
EXPOSE 80