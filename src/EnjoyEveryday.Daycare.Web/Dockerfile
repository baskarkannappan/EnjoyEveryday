# ==========================================
# Stage 1: Build & Restore
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for efficient dependency caching
COPY ["src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj", "src/EnjoyEveryday.Daycare.Web/"]
COPY ["src/EnjoyEveryday.UI.Shared/EnjoyEveryday.UI.Shared.csproj", "src/EnjoyEveryday.UI.Shared/"]
COPY ["src/EnjoyEveryday.Application/EnjoyEveryday.Application.csproj", "src/EnjoyEveryday.Application/"]
COPY ["src/EnjoyEveryday.Infrastructure/EnjoyEveryday.Infrastructure.csproj", "src/EnjoyEveryday.Infrastructure/"]
COPY ["src/EnjoyEveryday.Domain/EnjoyEveryday.Domain.csproj", "src/EnjoyEveryday.Domain/"]
COPY ["src/EnjoyEveryday.Shared/EnjoyEveryday.Shared.csproj", "src/EnjoyEveryday.Shared/"]

RUN dotnet restore "src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj"

# Copy remaining source code and compile
COPY . .
WORKDIR "/src/src/EnjoyEveryday.Daycare.Web"
RUN dotnet build "EnjoyEveryday.Daycare.Web.csproj" -c Release -o /app/build

# ==========================================
# Stage 2: Publish
# ==========================================
FROM build AS publish
RUN dotnet publish "EnjoyEveryday.Daycare.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Stage 3: Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnjoyEveryday.Daycare.Web.dll"]

