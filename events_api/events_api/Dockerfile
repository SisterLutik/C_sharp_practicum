# ============================================
# Build stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj-ы для кэширования restore
COPY ["src/EventsApi.Contracts/EventsApi.Contracts.csproj", "src/EventsApi.Contracts/"]
COPY ["src/UserService/UserService.Domain/UserService.Domain.csproj", "src/UserService/UserService.Domain/"]
COPY ["src/UserService/UserService.Application/UserService.Application.csproj", "src/UserService/UserService.Application/"]
COPY ["src/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "src/UserService/UserService.Infrastructure/"]
COPY ["src/UserService/UserService.Presentation/UserService.Presentation.csproj", "src/UserService/UserService.Presentation/"]

COPY ["src/EventService/EventService.Domain/EventService.Domain.csproj", "src/EventService/EventService.Domain/"]
COPY ["src/EventService/EventService.Application/EventService.Application.csproj", "src/EventService/EventService.Application/"]
COPY ["src/EventService/EventService.Infrastructure/EventService.Infrastructure.csproj", "src/EventService/EventService.Infrastructure/"]
COPY ["src/EventService/EventService.Presentation/EventService.Presentation.csproj", "src/EventService/EventService.Presentation/"]

COPY ["src/BookingService/BookingService.Domain/BookingService.Domain.csproj", "src/BookingService/BookingService.Domain/"]
COPY ["src/BookingService/BookingService.Application/BookingService.Application.csproj", "src/BookingService/BookingService.Application/"]
COPY ["src/BookingService/BookingService.Infrastructure/BookingService.Infrastructure.csproj", "src/BookingService/BookingService.Infrastructure/"]
COPY ["src/BookingService/BookingService.Presentation/BookingService.Presentation.csproj", "src/BookingService/BookingService.Presentation/"]

RUN dotnet restore "src/UserService/UserService.Presentation/UserService.Presentation.csproj"
RUN dotnet restore "src/EventService/EventService.Presentation/EventService.Presentation.csproj"
RUN dotnet restore "src/BookingService/BookingService.Presentation/BookingService.Presentation.csproj"

# Копируем весь исходный код
COPY src/ src/

# Публикуем выбранный сервис (через ARG)
ARG PROJECT
RUN dotnet publish "${PROJECT}" -c Release -o /app/publish

# ============================================
# Runtime stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserService.Presentation.dll"]