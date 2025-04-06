FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MailService/MailService.csproj", "src/MailService/"]
RUN dotnet restore "src/MailService/MailService.csproj"
COPY . .
WORKDIR "/src/src/MailService"
RUN dotnet build "MailService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MailService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MailService.dll"] 