FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["LangCenter.Domain/LangCenter.Domain.csproj", "LangCenter.Domain/"]
COPY ["LangCenter.Application/LangCenter.Application.csproj", "LangCenter.Application/"]
COPY ["LangCenter.Infrastructure/LangCenter.Infrastructure.csproj", "LangCenter.Infrastructure/"]
COPY ["LangCenter.API/LangCenter.API.csproj", "LangCenter.API/"]
RUN dotnet restore "LangCenter.API/LangCenter.API.csproj"

COPY LangCenter.Domain/ LangCenter.Domain/
COPY LangCenter.Application/ LangCenter.Application/
COPY LangCenter.Infrastructure/ LangCenter.Infrastructure/
COPY LangCenter.API/ LangCenter.API/
RUN dotnet publish "LangCenter.API/LangCenter.API.csproj" --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY ["LangCenter.API/healthcheck.sh", "./"]
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "LangCenter.API.dll"]
