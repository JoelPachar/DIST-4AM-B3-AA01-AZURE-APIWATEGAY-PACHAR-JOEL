# 🚀 Aplicaciones Distribuidas - Microservicios en Azure Container Apps

**Autor:** Joel Pachar

Este proyecto es un sistema de **microservicios distribuidos** para gestionar categorías y vehículos, con autenticación centralizada, comunicación asíncrona entre servicios (RabbitMQ) y un API Gateway que unifica el acceso a todo. Todo el sistema corre en **Azure Container Apps**, usando **Azure SQL Database** como base de datos y **Azure Container Registry (ACR)** para almacenar las imágenes Docker.

---

## 🧩 ¿Qué hace este sistema?

Es un panel web donde un usuario puede iniciar sesión, ver y crear **categorías** de vehículos, y ver **vehículos**. Lo interesante es que **cada vez que se crea una categoría nueva, automáticamente se genera un vehículo de prueba asociado a esa categoría**, sin que el usuario tenga que hacerlo manualmente. Esto sucede gracias a un sistema de mensajería (RabbitMQ) que comunica el microservicio de Categorías con el de Vehículos.

---

## 🏗️ Tecnologías utilizadas

| Tecnología | Para qué se usa |
|---|---|
| **ASP.NET Core Web API (.NET)** | Backend de cada microservicio |
| **Azure SQL Database** | Base de datos de Categorías y de Vehículos |
| **RabbitMQ** | Mensajería asíncrona entre microservicios |
| **YARP (Yet Another Reverse Proxy)** | Motor del API Gateway |
| **JWT (JSON Web Tokens)** | Autenticación y control de roles |
| **Docker** | Empaquetar cada servicio como contenedor |
| **Azure Container Registry (ACR)** | Almacenar las imágenes Docker |
| **Azure Container Apps** | Ejecutar y exponer todos los servicios en la nube |
| **Nginx** | Servir el frontend (HTML/CSS/JS) |
| **Swagger** | Documentación interactiva de cada API |

---

## 📂 Estructura del proyecto

```text
/
├── ApiGatewayA/        → API Gateway (YARP) - enruta todo el tráfico
├── Seguridad.api/       → Microservicio de autenticación (login + JWT)
├── Categoria/           → Microservicio de Categorías (CRUD + publica eventos)
├── Vehiculo/            → Microservicio de Vehículos (CRUD + consume eventos)
├── Frontend/            → Interfaz web (HTML/CSS/JS) servida con Nginx
├── BaseDatos/           → Scripts SQL para crear las bases de datos
└── README.md
```

---

## ⚙️ Arquitectura general

```text
                        [ Usuario / Navegador ]
                                 │
                                 ▼
                 [ Frontend Web (Nginx) - HTML/JS ]
                                 │  (fetch a la API)
                                 ▼
                    [ API Gateway (YARP) ]
                                 │
        ┌───────────────┬───────┴────────┬────────────────┐
        ▼               ▼                ▼
[ Seguridad.Api ]  [ Categoria.Api ]  [ Vehiculo.Api ]
   (Login/JWT)          │                  ▲
                         │  publica evento  │  consume evento
                         ▼                  │
                   [ RabbitMQ (cola "Deber") ]
                         │
              ┌──────────┴──────────┐
              ▼                     ▼
     [ Azure SQL - BD Categoria ] [ Azure SQL - BD Vehiculo ]
        (mismo servidor lógico: sql-deber-vehiculo)
```

### 🔄 Flujo automático Categoría → Vehículo

1. El usuario (o el frontend) crea una categoría nueva vía `POST /api/Categorias`.
2. `Categoria.Api` guarda la categoría en su base de datos.
3. `Categoria.Api` publica un mensaje a la cola `Deber` en RabbitMQ con los datos de la categoría creada.
4. `Vehiculo.Api` está escuchando esa misma cola. Al recibir el mensaje, crea automáticamente un vehículo de prueba asociado a esa categoría (`Marca: "Generado Automáticamente"`) en su propia base de datos.
5. Si consultas `GET /api/Vehiculo`, verás ese vehículo generado sin haberlo creado manualmente.

---

## 🖥️ Descripción de cada microservicio

### 1. 🔐 Seguridad.Api (Autenticación)
Genera el token JWT que los demás servicios usan para validar quién eres y qué rol tienes.

- **Endpoint:** `POST /api/Auth/login`
- **Body de ejemplo:**
  ```json
  { "usuario": "admin", "password": "1234" }
  ```
- **Usuarios de prueba:**
  | Usuario | Contraseña | Rol |
  |---|---|---|
  | `admin` | `1234` | Administrador |
  | `usuario` | `1234` | Usuario |
- **Respuesta:** un token JWT, el nombre de usuario y el rol. Este token debe enviarse en el header `Authorization: Bearer <token>` en todas las peticiones a los demás microservicios.

### 2. 🏷️ Categoria.Api
Gestiona el CRUD de categorías y publica el evento a RabbitMQ al crear una.

- `GET /api/Categorias` → listar (cualquier usuario autenticado)
- `GET /api/Categorias/{id}` → obtener una
- `POST /api/Categorias` → crear (solo rol Administrador)
- `PUT /api/Categorias/{id}` → actualizar (solo rol Administrador)
- `DELETE /api/Categorias/{id}` → eliminar (solo rol Administrador)

### 3. 🚗 Vehiculo.Api
Gestiona el CRUD de vehículos y contiene un "consumidor" en segundo plano que escucha la cola de RabbitMQ para crear vehículos automáticamente.

- `GET /api/Vehiculo` → listar
- `POST /api/Vehiculo` → crear vehículo manualmente

### 4. 🔀 ApiGatewayA (API Gateway)
Es la **única puerta de entrada** para el frontend. Recibe todas las peticiones y las redirige (enruta) al microservicio correcto según la ruta:

| Ruta que recibe el Gateway | La reenvía a |
|---|---|
| `/api/Auth/**` | Seguridad.Api |
| `/api/Categorias/**` | Categoria.Api |
| `/api/Vehiculo/**` | Vehiculo.Api |

También valida el token JWT antes de dejar pasar la petición (excepto en `/api/Auth/login`, que es pública).

### 5. 🐇 RabbitMQ
Es el "cartero" que lleva el mensaje de "se creó una categoría" desde Categoria.Api hasta Vehiculo.Api, sin que ambos servicios tengan que conocerse directamente entre sí. Corre como un contenedor interno (no accesible desde internet), usando la imagen oficial `rabbitmq:3-management`.

### 6. 🌐 Frontend
Una página HTML simple con formularios de login, creación de categorías y vehículos, y tablas para listarlos. Se sirve como archivos estáticos usando Nginx. Todas las peticiones del frontend van dirigidas al **API Gateway**, nunca directamente a los microservicios.

---

## ☁️ Recursos de Azure utilizados

| Recurso | Nombre en este proyecto | Para qué sirve |
|---|---|---|
| **Resource Group** | `DistribuidasRG` | Agrupa todos los recursos del proyecto |
| **Container Apps Environment** | `env-distribuidos-deber` | El "espacio" donde viven todos los contenedores |
| **Azure Container Registry (ACR)** | `acrdistrrabbitmqapigatewayvehiculo` | Almacena las imágenes Docker de cada servicio |
| **Servidor lógico Azure SQL** | `sql-deber-vehiculo.database.windows.net` | Un solo servidor que aloja las dos bases de datos |
| **Bases de datos** | `Vehiculo`, `Categoria` | Cada microservicio tiene su propia base dentro del mismo servidor |

### Container Apps desplegadas

| Container App | Puerto interno | Acceso |
|---|---|---|
| `seguridad-api` | 8080 | Externo (público) |
| `categoria-api` | 8080 | Externo (público) |
| `vehiculo-api` | 8080 | Externo (público) |
| `rabbitmq-srv` | 5672 | Interno (solo visible entre servicios) |
| `apigateway-api` | 8080 | Externo (público) — **punto de entrada principal** |
| `frontend-web` | 80 | Externo (público) |

---

## 🚀 Cómo desplegar un servicio nuevo (resumen del proceso)

Cada microservicio de este proyecto se desplegó siguiendo estos mismos pasos generales:

1. **Iniciar sesión en el registro de contenedores:**
   ```powershell
   az acr login --name acrdistrrabbitmqapigatewayvehiculo
   ```

2. **Construir la imagen Docker** desde la carpeta del proyecto (donde está el `Dockerfile`):
   ```powershell
   docker build -t acrdistrrabbitmqapigatewayvehiculo.azurecr.io/<nombre-servicio>:v1 .
   ```

3. **Subir la imagen al registro:**
   ```powershell
   docker push acrdistrrabbitmqapigatewayvehiculo.azurecr.io/<nombre-servicio>:v1
   ```

4. **Crear la Container App:**
   ```powershell
   az containerapp create `
     --name <nombre-servicio> `
     --resource-group DistribuidasRG `
     --environment env-distribuidos-deber `
     --image acrdistrrabbitmqapigatewayvehiculo.azurecr.io/<nombre-servicio>:v1 `
     --registry-server acrdistrrabbitmqapigatewayvehiculo.azurecr.io `
     --target-port 8080 `
     --ingress external `
     --min-replicas 1 `
     --max-replicas 1
   ```

5. **Configurar variables de entorno** (connection strings, JWT, RabbitMQ, etc.) usando `secretref` para datos sensibles:
   ```powershell
   az containerapp secret set --name <nombre-servicio> --resource-group DistribuidasRG --secrets "mi-secreto=<valor>"
   az containerapp update --name <nombre-servicio> --resource-group DistribuidasRG --set-env-vars "Variable=secretref:mi-secreto"
   ```

6. **Si algo falla al iniciar,** revisar los logs y el estado de salud de la revisión:
   ```powershell
   az containerapp logs show --name <nombre-servicio> --resource-group DistribuidasRG --tail 100
   az containerapp revision list --name <nombre-servicio> --resource-group DistribuidasRG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table
   ```

---

## 🔑 Variables de entorno por servicio

### seguridad-api
```
Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!
Jwt__Issuer=http://localhost:5003
Jwt__Audience=http://localhost:5003
Jwt__DurationInMinutes=60
```

### categoria-api
```
ConnectionStrings__CategoriaConnection=secretref:categoria-sql
Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!
Jwt__Issuer=http://localhost:5003
Jwt__Audience=http://localhost:5003
RabbitMQ__HostName=rabbitmq-srv
RabbitMQ__Port=5672
RabbitMQ__UserName=admin
RabbitMQ__Password=admin123
RabbitMQ__QueueName=Deber
```

### vehiculo-api
```
ConnectionStrings__VehiculoConnection=secretref:vehiculo-sql
Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!
Jwt__Issuer=http://localhost:5003
Jwt__Audience=http://localhost:5003
RabbitMQ__HostName=rabbitmq-srv
RabbitMQ__Port=5672
RabbitMQ__UserName=admin
RabbitMQ__Password=admin123
RabbitMQ__QueueName=Deber
```

### rabbitmq-srv
```
RABBITMQ_DEFAULT_USER=admin
RABBITMQ_DEFAULT_PASS=admin123
```

### apigateway-api
```
Jwt__Key=CLAVE_SECRETA_LOCAL_PARA_PRUEBAS_12345!
Jwt__Issuer=http://localhost:5003
Jwt__Audience=http://localhost:5003
ReverseProxy__Clusters__seguridadCluster__Destinations__seguridadDestination__Address=https://seguridad-api.calmrock-4e160d8a.eastus.azurecontainerapps.io
ReverseProxy__Clusters__categoriasCluster__Destinations__categoriasDestination__Address=https://categoria-api.calmrock-4e160d8a.eastus.azurecontainerapps.io
ReverseProxy__Clusters__vehiculosCluster__Destinations__vehiculosDestination__Address=https://vehiculo-api.calmrock-4e160d8a.eastus.azurecontainerapps.io
```

> ⚠️ **Importante:** el `Jwt:Key`, `Jwt:Issuer` y `Jwt:Audience` deben ser **exactamente iguales** en `seguridad-api`, `categoria-api`, `vehiculo-api` y `apigateway-api`. Si no coinciden, el token generado en el login no será válido cuando se valide en los demás servicios.

---

## 🌐 Cómo probar todo el sistema

### Opción 1: Desde el navegador (recomendado)
Abre la URL pública del frontend:
```
https://frontend-web.calmrock-4e160d8a.eastus.azurecontainerapps.io
```
Inicia sesión con `admin` / `1234` y prueba crear categorías y ver los vehículos generados automáticamente.

### Opción 2: Desde Postman / Swagger, paso a paso

1. **Login** (sin token):
   ```
   POST https://apigateway-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/api/Auth/login
   Body: { "usuario": "admin", "password": "1234" }
   ```
2. Copia el `token` que te devuelve.
3. **Listar categorías** (con el token en el header `Authorization: Bearer <token>`):
   ```
   GET https://apigateway-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/api/Categorias
   ```
4. **Crear una categoría nueva:**
   ```
   POST https://apigateway-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/api/Categorias
   Body: { "nombre": "SUV", "descripcion": "Vehículos utilitarios", "estado": true }
   ```
5. **Verificar que se generó un vehículo automáticamente:**
   ```
   GET https://apigateway-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/api/Vehiculo
   ```

### URLs directas de cada servicio (para depuración, sin pasar por el Gateway)

| Servicio | Swagger |
|---|---|
| Seguridad | `https://seguridad-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/swagger/index.html` |
| Categoría | `https://categoria-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/swagger/index.html` |
| Vehículo | `https://vehiculo-api.calmrock-4e160d8a.eastus.azurecontainerapps.io/swagger/index.html` |

---

## 🛠️ Problemas comunes y cómo resolverlos

| Síntoma | Causa probable | Solución |
|---|---|---|
| `Login failed for user 'X'` (SQL) | El usuario no tiene permisos en esa base de datos específica | Conectarse con el admin a la base correspondiente y ejecutar `CREATE USER X WITH PASSWORD = '...'; ALTER ROLE db_owner ADD MEMBER X;` |
| `Name or service not known` (SQL) | El nombre del servidor en el connection string está mal escrito o no existe | Verificar el FQDN real con `az sql server list` |
| `ACCESS_REFUSED` (RabbitMQ) | Usuario/contraseña de RabbitMQ no configurados o incorrectos en ese servicio | Verificar `RabbitMQ__UserName` / `RabbitMQ__Password` en las variables de entorno de cada Container App |
| La API se cae apenas arranca (revisión `Unhealthy`/`Failed`) | Un `BackgroundService` (como el consumer de RabbitMQ) lanza una excepción no controlada al iniciar, y por defecto eso detiene toda la aplicación | Revisar logs con `az containerapp logs show`; corregir la causa raíz (credenciales, conexión) |
| Cambié un secreto pero el error sigue igual | La Container App no se reinició, sigue sirviendo desde la revisión vieja | Forzar reinicio: `az containerapp revision restart --name <servicio> --resource-group DistribuidasRG --revision <nombre-revision>` |
| El Gateway da error de conexión hacia un microservicio | La variable `ReverseProxy__Clusters__...__Address` no apunta al FQDN correcto | Verificar el FQDN real del servicio destino y actualizar la variable de entorno del Gateway |
| Postman da `getaddrinfo ENOTFOUND` | Se dejó un placeholder tipo `<fqdn-...>` sin reemplazar por la URL real | Reemplazar por el dominio real devuelto por `az containerapp show ... --query "properties.configuration.ingress.fqdn"` |

---

## 🔍 Comandos útiles de diagnóstico

```powershell
# Ver el estado de salud y tráfico de las revisiones de un servicio
az containerapp revision list --name <servicio> --resource-group DistribuidasRG --query "[].{Nombre:name, Activa:properties.active, Salud:properties.healthState, Trafico:properties.trafficWeight}" -o table

# Ver los logs recientes de un servicio
az containerapp logs show --name <servicio> --resource-group DistribuidasRG --tail 100

# Ver las variables de entorno configuradas actualmente
az containerapp show --name <servicio> --resource-group DistribuidasRG --query "properties.template.containers[0].env"

# Obtener la URL pública de un servicio
az containerapp show --name <servicio> --resource-group DistribuidasRG --query "properties.configuration.ingress.fqdn" -o tsv

# Ejecutar comandos dentro del contenedor de RabbitMQ (ej. ver usuarios o colas)
az containerapp exec --name rabbitmq-srv --resource-group DistribuidasRG --command "rabbitmqctl list_users"
az containerapp exec --name rabbitmq-srv --resource-group DistribuidasRG --command "rabbitmqctl list_queues name messages consumers"
```