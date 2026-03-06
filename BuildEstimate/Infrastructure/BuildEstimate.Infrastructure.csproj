<!--
  BuildEstimate.Infrastructure.csproj
  
  This is the ONLY project that knows about databases.
  It has Entity Framework Core and the SQL Server provider.
  
  JERP EQUIVALENT: JERP.Infrastructure.csproj
-->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Authors>Julio Cesar Mendez Tobar</Authors>
    <Company>Julio Cesar Mendez Tobar</Company>
  </PropertyGroup>

  <ItemGroup>
    <!-- Entity Framework Core — the ORM that maps C# to SQL -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    
    <!-- SQL Server provider — tells EF how to talk to SQL Server specifically -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    
    <!-- Design-time tools — needed for creating migrations -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    
    <!-- EF Tools — the dotnet ef command line tool support -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <!-- Infrastructure depends on Core (for entity types used in DbContext) -->
  <ItemGroup>
    <ProjectReference Include="..\Core\BuildEstimate.Core.csproj" />
  </ItemGroup>

</Project>
