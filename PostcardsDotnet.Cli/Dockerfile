FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PostcardsDotnet.Cli/PostcardsDotnet.Cli.csproj", "PostcardsDotnet.Cli/"]
RUN dotnet restore "PostcardsDotnet.Cli/PostcardsDotnet.Cli.csproj"
COPY . .
WORKDIR "/src/PostcardsDotnet.Cli"
RUN dotnet build "PostcardsDotnet.Cli.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PostcardsDotnet.Cli.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PostcardsDotnet.Cli.dll"]
