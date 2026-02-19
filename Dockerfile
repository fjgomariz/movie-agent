# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY src/MovieAgent/MovieAgent.csproj src/MovieAgent/
RUN dotnet restore src/MovieAgent/MovieAgent.csproj

# Copy source code and build
COPY src/MovieAgent/ src/MovieAgent/
WORKDIR /src/src/MovieAgent
RUN dotnet build MovieAgent.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish MovieAgent.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/healthz || exit 1

# Start the application
ENTRYPOINT ["dotnet", "MovieAgent.dll"]
