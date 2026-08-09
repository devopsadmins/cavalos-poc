# Deploy no IIS - CavalosPOC

## 1. Pré-requisitos no Servidor

### Instalar .NET Hosting Bundle
Baixar e instalar: https://dotnet.microsoft.com/download/dotnet/10.0/runtime
- Escolher: **ASP.NET Core Runtime** → **Hosting Bundle** (instala runtime + módulo IIS)

### Habilitar Recursos IIS (PowerShell Admin)
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName `
  IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-StaticContent,IIS-DefaultDocument,`
  IIS-DirectoryBrowsing,IIS-HttpErrors,IIS-HttpRedirect,IIS-ApplicationDevelopment,`
  IIS-NetFxExtensibility45,IIS-HealthAndDiagnostics,IIS-HttpLogging,IIS-LoggingLibraries,`
  IIS-RequestMonitor,IIS-Security,IIS-RequestFiltering,IIS-Performance,`
  IIS-HttpCompressionStatic,IIS-WebServerManagementTools,IIS-ManagementConsole,`
  IIS-ManagementService,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-ASPNET45 -All
```

Reiniciar IIS:
```powershell
iisreset
```

---

## 2. Publicar a Aplicação

### Opção A: Linha de comando
```bash
dotnet publish CavalosPOC.csproj -c Release -o C:\publish\CavalosPOC
```

### Opção B: Visual Studio
- Botão direito no projeto → **Publish**
- Target: **Folder**
- Location: `C:\publish\CavalosPOC`
- Configuration: **Release**
- Deploy mode: **Self-contained** (recomendado para evitar conflitos de versão)

---

## 3. Configurar no IIS

### 3.1 Application Pool
1. Abrir **IIS Manager** (`inetmgr`)
2. **Application Pools** → **Add Application Pool**
   - Name: `CavalosPOC`
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
   - Start Mode: **AlwaysRunning** (opcional, para warm-up)

3. **Advanced Settings** do pool:
   - Identity: `ApplicationPoolIdentity` (ou conta de serviço dedicada)
   - Idle Time-out: `0` (desliga reciclagem por inatividade)
   - Recycling → Regular Time Interval: `0` (configurar horário específico se necessário)

### 3.2 Site
1. **Sites** → **Add Website**
   - Site name: `CavalosPOC`
   - Application Pool: `CavalosPOC` (criado acima)
   - Physical path: `C:\publish\CavalosPOC`
   - Binding: HTTP porta 80 (ou 443 para HTTPS)
   - Host name: `cavalospoc.seudominio.com` (opcional)

### 3.3 Permissões de Pasta
```powershell
icacls "C:\publish\CavalosPOC" /grant "IIS_IUSRS:(OI)(CI)RX" /T
```

---

## 4. Configuração de Conexão (Produção)

### appsettings.Production.json
Criar na pasta de publish (`C:\publish\CavalosPOC`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=seu-oracle-host)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=SEU_SID)));User Id=seu_user;Password=sua_senha;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CavalosPOC.Data.CavaloRepository": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

> **Segurança**: Não comitar este arquivo. Gerenciar via pipeline CI/CD ou configurar variáveis de ambiente no App Pool.

### Variáveis de Ambiente (Alternativa)
No **App Pool → Advanced Settings → Environment Variables**:
```
ConnectionStrings__DefaultConnection = Data Source=...;User Id=...;Password=...
```

---

## 5. HTTPS (Recomendado)

### Binding HTTPS no IIS
1. Site → **Bindings** → **Add**
   - Type: `https`
   - Port: `443`
   - IP Address: `All Unassigned`
   - Host name: `cavalospoc.seudominio.com`
   - SSL Certificate: Selecionar certificado válido (PFX instalado no Windows Certificate Store)

### HSTS e Redirecionamento (Program.cs já configurado)
```csharp
app.UseHttpsRedirection(); // Já presente
```

---

## 6. Verificação

### Testar localmente no servidor
```powershell
Invoke-WebRequest -Uri "http://localhost/api/Cavalos/buscar?nome=teste" -UseBasicParsing
```

### Logs
- **Stdout**: `C:\publish\CavalosPOC\logs\stdout_*.log` (se configurado)
- **Event Viewer**: Windows Logs → Application (fonte: `IIS AspNetCore Module`)
- **ASP.NET Core Module**: `%PROGRAMFILES%\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll`

---

## 7. Troubleshooting Comum

| Problema | Solução |
|----------|---------|
| **500.30 / 500.31** | Verificar se Hosting Bundle instalado, versão .NET correta, `web.config` presente |
| **500.19** | Verificar permissões pasta, `web.config` válido |
| **Connection String** | Testar `tnsping` do servidor Oracle, firewall porta 1521 |
| **Logs não aparecem** | Verificar `stdoutLogEnabled="true"` no `web.config`, pasta `logs` com permissão escrita |
| **Reciclagem constante** | Ajustar Idle Time-out = 0, configurar horário fixo de reciclagem |

---

## 8. Atualização (Zero-downtime)

```bash
# 1. Publicar em pasta temporária
dotnet publish -c Release -o C:\publish\CavalosPOC_new

# 2. Trocar pasta atomicamente (PowerShell)
Rename-Item C:\publish\CavalosPOC C:\publish\CavalosPOC_old -Force
Rename-Item C:\publish\CavalosPOC_new C:\publish\CavalosPOC

# 3. Reciclar App Pool
Restart-WebAppPool CavalosPOC

# 4. Limpar antigo após validar
Remove-Item C:\publish\CavalosPOC_old -Recurse -Force
```

---

## 9. Checklist de Produção

- [ ] Hosting Bundle .NET 10 instalado
- [ ] IIS configurado com App Pool "No Managed Code"
- [ ] Site apontando para pasta de publish
- [ ] Permissões `IIS_IUSRS` na pasta
- [ ] `appsettings.Production.json` com connection string real
- [ ] HTTPS configurado com certificado válido
- [ ] Firewall Oracle (porta 1521) liberado
- [ ] Logs funcionando (testar endpoint)
- [ ] Health check endpoint respondendo (opcional: adicionar `/health`)
- [ ] Backup/Restore testado