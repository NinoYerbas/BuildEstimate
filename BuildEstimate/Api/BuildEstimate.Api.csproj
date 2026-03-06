<!--
  BuildEstimate.Api.csproj — The Web API Project
  
  This is the project that actually RUNS.
  The others (Core, Application, Infrastructure) are class libraries.
  This one is a web application that listens for HTTP requests.
  
  JERP EQUIVALENT: JERP_Api.csproj — your main API project.
-->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <!--                          ^^^
    Notice: "Microsoft.NET.Sdk.Web" not "Microsoft.NET.Sdk"
    The .Web SDK includes ASP.NET Core — web server, routing, controllers, etc.
    Only the API project uses this; the others use the plain SDK.
  -->

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Version>1.0.0</Version>
    <Authors>Julio Cesar Mendez Tobar</Authors>
    <Company>Julio Cesar Mendez Tobar</Company>
    <Product>BuildEstimate — Construction Estimating Software</Product>
    <Copyright>Copyright © 2026 Julio Cesar Mendez Tobar. All Rights Reserved.</Copyright>
    <Description>Construction cost estimating system with CSI MasterFormat, prevailing wages, and AI integration</Description>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core Authentication — JWT tokens (same as JERP) -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    
    <!-- Swagger — auto-generated API documentation (same as JERP) -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="6.5.0" />
    
    <!-- Serilog — structured logging (same as JERP) -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
    
    <!-- Health Checks (same as JERP) -->
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.0.0" />
  </ItemGroup>

  <!-- References to all other projects in the solution -->
  <ItemGroup>
    <ProjectReference Include="..\Core\BuildEstimate.Core.csproj" />
    <ProjectReference Include="..\Application\BuildEstimate.Application.csproj" />
    <ProjectReference Include="..\Infrastructure\BuildEstimate.Infrastructure.csproj" />
  </ItemGroup>

</Project>
