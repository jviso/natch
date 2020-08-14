
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

LABEL maintainer="Jacob Visovatti <jacob@deepgram.com>"

COPY bin/Release/netcoreapp3.1/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "Natch.dll"]
