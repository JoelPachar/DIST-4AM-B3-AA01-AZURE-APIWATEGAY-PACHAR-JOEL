# 📋 Memoria de Comandos — Despliegue desde Cero

Guía con **todos los comandos en orden**, listos para copiar y pegar, para desplegar los 6 servicios en Azure Container Apps: **RabbitMQ, Categoría, Vehículo, Seguridad, API Gateway y Frontend**.

> ⚠️ Esta guía asume que las bases de datos (`Categoria` y `Vehiculo`) **ya existen** en Azure SQL con sus usuarios y permisos configurados (eso no se cubre aquí).

---

## 🔧 Variables generales (ajusta si cambian)

```powershell
$RG = "DistribuidasRG"
$ENV = "env-distribuidos-deber"
$ACR = "acrdistrrabbitmqapigatewayvehiculo"
$ACR_LOGIN = "$ACR.azurecr.io"
```

**Iniciar sesión en Azure y en el ACR (una sola vez por sesión de terminal):**
```powershell
az login
az acr login --name $ACR
```

---

## 1️⃣ RabbitMQ (`rabbitmq-srv`)

No requiere build de imagen propia, se usa la imagen oficial directamente.

```powershell
az containerapp create `
  --name rabbitmq-srv `
  --resource-group $RG `
  --environment $ENV `
  --image rabbitmq:3-management `
  --target-port 5672 `
  --ingress internal `
  --min-replicas 1 `
  --max-replicas 1
```

**Configurar usuario y contraseña:**
```powershell
az containerapp update --name rabbitmq-srv --resource-group $RG --set-env-vars `
  "RABBITMQ_DEFAULT_USER=admin" `
  "RABBITMQ_DEFAULT_PASS=admin123"
```

**Verificar salud:**
```powershell
az containerapp revision list --name rabbitmq-srv --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

---

## 2️⃣ Categoría (`categoria-api`)

**Build y push de la imagen** (desde la carpeta del proyecto Categoria, donde está el `Dockerfile`):
```powershell
cd D:\Distribuidas\practica_AZURE\Categoria
docker build -t $ACR_LOGIN/categoria-api:v1 .
docker push $ACR_LOGIN/categoria-api:v1
```

**Crear el secreto de la connection string:**
```powershell
$bC = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$bC["Data Source"] = "tcp:sql-deber-vehiculo.database.windows.net,1433"
$bC["Initial Catalog"] = "Categoria"
$bC["User ID"] = "UserLibroDBA"
$bC["Password"] = "xVsVI/uw59#7"
$bC["Encrypt"] = $true
$bC["TrustServerCertificate"] = $true
$connCategoriaReal = $bC.ConnectionString

az containerapp create `
  --name categoria-api `
  --resource-group $RG `
  --environment $ENV `
  --image $ACR_LOGIN/categoria-api:v1 `
  --registry-server $ACR_LOGIN `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 1 `
  --secrets "categoria-sql=$connCategoriaReal"
```

**Configurar variables de entorno:**
```powershell
az containerapp update --name categoria-api --resource-group $RG --set-env-vars `
  "ASPNETCORE_ENVIRONMENT=Development" `
  "ConnectionStrings__CategoriaConnection=secretref:categoria-sql" `
  "Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!" `
  "Jwt__Issuer=http://localhost:5003" `
  "Jwt__Audience=http://localhost:5003" `
  "RabbitMQ__HostName=rabbitmq-srv" `
  "RabbitMQ__Port=5672" `
  "RabbitMQ__UserName=admin" `
  "RabbitMQ__Password=admin123" `
  "RabbitMQ__QueueName=Deber"
```

**Verificar salud:**
```powershell
az containerapp revision list --name categoria-api --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

**Obtener URL pública:**
```powershell
az containerapp show --name categoria-api --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## 3️⃣ Vehículo (`vehiculo-api`)

**Build y push de la imagen:**
```powershell
cd D:\Distribuidas\practica_AZURE\Vehiculo
docker build -t $ACR_LOGIN/vehiculo-api:v1 .
docker push $ACR_LOGIN/vehiculo-api:v1
```

**Crear el secreto de la connection string:**
```powershell
$bV = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$bV["Data Source"] = "tcp:sql-deber-vehiculo.database.windows.net,1433"
$bV["Initial Catalog"] = "Vehiculo"
$bV["User ID"] = "curso4a"
$bV["Password"] = "w4Vo9~c3Rzv$"
$bV["Encrypt"] = $true
$bV["TrustServerCertificate"] = $true
$connVehiculoReal = $bV.ConnectionString

az containerapp create `
  --name vehiculo-api `
  --resource-group $RG `
  --environment $ENV `
  --image $ACR_LOGIN/vehiculo-api:v1 `
  --registry-server $ACR_LOGIN `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 1 `
  --secrets "vehiculo-sql=$connVehiculoReal"
```

**Configurar variables de entorno:**
```powershell
az containerapp update --name vehiculo-api --resource-group $RG --set-env-vars `
  "ASPNETCORE_ENVIRONMENT=Development" `
  "ConnectionStrings__VehiculoConnection=secretref:vehiculo-sql" `
  "Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!" `
  "Jwt__Issuer=http://localhost:5003" `
  "Jwt__Audience=http://localhost:5003" `
  "RabbitMQ__HostName=rabbitmq-srv" `
  "RabbitMQ__Port=5672" `
  "RabbitMQ__UserName=admin" `
  "RabbitMQ__Password=admin123" `
  "RabbitMQ__QueueName=Deber"
```

**Verificar salud:**
```powershell
az containerapp revision list --name vehiculo-api --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

**Obtener URL pública:**
```powershell
az containerapp show --name vehiculo-api --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## 4️⃣ Seguridad (`seguridad-api`)

**Build y push de la imagen:**
```powershell
cd D:\Distribuidas\practica_AZURE\Seguridad.api
docker build -t $ACR_LOGIN/seguridad-api:v1 .
docker push $ACR_LOGIN/seguridad-api:v1
```

**Crear la Container App:**
```powershell
az containerapp create `
  --name seguridad-api `
  --resource-group $RG `
  --environment $ENV `
  --image $ACR_LOGIN/seguridad-api:v1 `
  --registry-server $ACR_LOGIN `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 1
```

**Configurar variables de entorno:**
```powershell
az containerapp update --name seguridad-api --resource-group $RG --set-env-vars `
  "ASPNETCORE_ENVIRONMENT=Development" `
  "Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!" `
  "Jwt__Issuer=http://localhost:5003" `
  "Jwt__Audience=http://localhost:5003" `
  "Jwt__DurationInMinutes=60"
```

**Verificar salud:**
```powershell
az containerapp revision list --name seguridad-api --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

**Obtener URL pública:**
```powershell
az containerapp show --name seguridad-api --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## 5️⃣ API Gateway (`apigateway-api`)

> Necesitas los FQDN de `seguridad-api`, `categoria-api` y `vehiculo-api` (obtenidos en los pasos anteriores) antes de este paso.

**Build y push de la imagen:**
```powershell
cd D:\Distribuidas\practica_AZURE\ApiGatewayA
docker build -t $ACR_LOGIN/apigateway-api:v1 .
docker push $ACR_LOGIN/apigateway-api:v1
```

**Crear la Container App:**
```powershell
az containerapp create `
  --name apigateway-api `
  --resource-group $RG `
  --environment $ENV `
  --image $ACR_LOGIN/apigateway-api:v1 `
  --registry-server $ACR_LOGIN `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 1
```

**Configurar variables de entorno** (reemplaza los FQDN por los reales obtenidos antes):
```powershell
az containerapp update --name apigateway-api --resource-group $RG --set-env-vars `
  "ASPNETCORE_ENVIRONMENT=Development" `
  "Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!" `
  "Jwt__Issuer=http://localhost:5003" `
  "Jwt__Audience=http://localhost:5003" `
  "ReverseProxy__Clusters__seguridadCluster__Destinations__seguridadDestination__Address=https://seguridad-api.calmrock-4e160d8a.eastus.azurecontainerapps.io" `
  "ReverseProxy__Clusters__categoriasCluster__Destinations__categoriasDestination__Address=https://categoria-api.calmrock-4e160d8a.eastus.azurecontainerapps.io" `
  "ReverseProxy__Clusters__vehiculosCluster__Destinations__vehiculosDestination__Address=https://vehiculo-api.calmrock-4e160d8a.eastus.azurecontainerapps.io"
```

**Verificar salud:**
```powershell
az containerapp revision list --name apigateway-api --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

**Obtener URL pública (esta es la URL principal del sistema):**
```powershell
az containerapp show --name apigateway-api --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## 6️⃣ Frontend (`frontend-web`)

> Antes de este paso, edita el `index.html` para que `API_BASE` apunte al FQDN real del `apigateway-api` obtenido arriba.

**Build y push de la imagen:**
```powershell
cd D:\Distribuidas\practica_AZURE\Frontend
docker build -t $ACR_LOGIN/frontend-web:v1 .
docker push $ACR_LOGIN/frontend-web:v1
```

**Crear la Container App:**
```powershell
az containerapp create `
  --name frontend-web `
  --resource-group $RG `
  --environment $ENV `
  --image $ACR_LOGIN/frontend-web:v1 `
  --registry-server $ACR_LOGIN `
  --target-port 80 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 1
```

**Verificar salud:**
```powershell
az containerapp revision list --name frontend-web --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
```

**Obtener URL pública (esta es la que abres en el navegador):**
```powershell
az containerapp show --name frontend-web --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## ✅ Orden recomendado de despliegue

```text
1. rabbitmq-srv       (los demás dependen de él)
2. categoria-api      (necesita rabbitmq-srv)
3. vehiculo-api       (necesita rabbitmq-srv)
4. seguridad-api      (independiente)
5. apigateway-api     (necesita los FQDN de los 3 anteriores)
6. frontend-web       (necesita el FQDN del apigateway-api)
```

---

## 🔁 Comandos de mantenimiento (por si algo falla tras un cambio)

**Forzar que un servicio relea una variable/secreto nuevo (reiniciar la revisión activa):**
```powershell
# 1. Ver cuál es la revisión activa
az containerapp revision list --name <servicio> --resource-group $RG --query "[].{Nombre:name, Activa:properties.active, Trafico:properties.trafficWeight}" -o table

# 2. Reiniciarla
az containerapp revision restart --name <servicio> --resource-group $RG --revision <nombre-revision-activa>
```

**Ver logs recientes de un servicio:**
```powershell
az containerapp logs show --name <servicio> --resource-group $RG --tail 100
```

**Actualizar un secreto existente:**
```powershell
az containerapp secret set --name <servicio> --resource-group $RG --secrets "nombre-secreto=<nuevo-valor>"
az containerapp revision restart --name <servicio> --resource-group $RG --revision <nombre-revision-activa>
```