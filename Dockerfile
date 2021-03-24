FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim

# Setting working directory
WORKDIR /app
# Copy the files from Release Folder to working directory
COPY ./bin/Release/net5.0/. /app/

RUN chmod +x "Assembly Bot.dll"

ENTRYPOINT ["dotnet", "Assembly Bot.dll"]