FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MessageReceiver.csproj", "./"]
RUN dotnet restore "MessageReceiver.csproj"
COPY . .
RUN dotnet build "MessageReceiver.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageReceiver.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MessageReceiver.dll"]

