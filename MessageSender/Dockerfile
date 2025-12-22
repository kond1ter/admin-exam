FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MessageSender.csproj", "./"]
RUN dotnet restore "MessageSender.csproj"
COPY . .
RUN dotnet build "MessageSender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageSender.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MessageSender.dll"]

