Write-Host "Iniciando Plataforma Educacional..." -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "CORREÇÕES APLICADAS: Bancos SQLite + Performance otimizada" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan

# Detectar e navegar para a raiz do projeto
$currentPath = Get-Location
$scriptPath = $PSScriptRoot
$projectRoot = Split-Path -Parent $scriptPath

# Se o script foi executado da pasta scripts, voltar para a raiz
if ($currentPath.Path.EndsWith("scripts")) {
    Set-Location -Path $projectRoot
    Write-Host "Navegando para a raiz do projeto: $projectRoot" -ForegroundColor Yellow
}

# Verificar se Docker esta rodando
try {
    docker info | Out-Null
    Write-Host "Docker esta rodando" -ForegroundColor Green
}
catch {
    Write-Host "Docker nao esta rodando. Por favor, inicie o Docker Desktop primeiro." -ForegroundColor Red
    exit 1
}

# Escolher compose file
$composeFile = ""
if (Test-Path "docker-compose-simple.yml") {
    $composeFile = "docker-compose-simple.yml"
    Write-Host "Usando docker-compose-simple.yml (DESENVOLVIMENTO)" -ForegroundColor Green
} elseif (Test-Path "docker-compose.yml") {
    $composeFile = "docker-compose.yml"
    Write-Host "Usando docker-compose.yml (PRODUCAO)" -ForegroundColor Green
} else {
    Write-Host "Arquivo docker-compose nao encontrado." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== ETAPA 1: PARANDO CONTAINERS EXISTENTES ===" -ForegroundColor Yellow

# Parar todos os containers existentes
Write-Host "Parando containers existentes..." -ForegroundColor Blue
docker-compose -f $composeFile down --remove-orphans
Write-Host "Containers parados e removidos" -ForegroundColor Green

Write-Host ""
Write-Host "=== ETAPA 2: LIMPEZA AGGRESSIVA DE ARQUIVOS SQLITE ===" -ForegroundColor Yellow

# Criar pasta data se não existir (para SQLite) com permissões adequadas
if (-not (Test-Path "data")) {
    New-Item -ItemType Directory -Path "data" | Out-Null
    Write-Host "Pasta data criada para arquivos SQLite" -ForegroundColor Green
}

# Verificar e corrigir permissões da pasta data (especialmente no Windows)
Write-Host "Verificando permissoes da pasta data..." -ForegroundColor Yellow
try {
    # No Windows, garantir que a pasta tem permissões adequadas
    $acl = Get-Acl "data"
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone","FullControl","ContainerInherit,ObjectInherit","None","Allow")
    $acl.SetAccessRule($rule)
    Set-Acl "data" $acl
    Write-Host "Permissoes da pasta data configuradas" -ForegroundColor Green
} catch {
    Write-Host "Aviso: Nao foi possivel configurar permissoes da pasta data" -ForegroundColor Yellow
}

# LIMPEZA AGGRESSIVA: Remover TODOS os arquivos SQLite existentes para evitar corrupção
Write-Host "LIMPANDO TODOS os arquivos SQLite existentes..." -ForegroundColor Red
$sqliteFiles = @("auth-dev.db", "conteudo-dev.db", "alunos-dev.db", "pagamentos-dev.db")
$sqliteExtensions = @("*.db", "*.db-shm", "*.db-wal", "*.db-journal")

foreach ($extension in $sqliteExtensions) {
    $files = Get-ChildItem -Path "data" -Filter $extension -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        try {
            Remove-Item $file.FullName -Force
            Write-Host "  Removido: $($file.Name)" -ForegroundColor Yellow
        } catch {
            Write-Host "  ERRO ao remover: $($file.Name)" -ForegroundColor Red
        }
    }
}

# Verificar se a pasta data está vazia
$remainingFiles = Get-ChildItem -Path "data" -File | Where-Object { $_.Name -match "\.db" }
if ($remainingFiles) {
    Write-Host "AVISO: Ainda existem arquivos SQLite na pasta data!" -ForegroundColor Red
    foreach ($file in $remainingFiles) {
        Write-Host "  Arquivo restante: $($file.Name)" -ForegroundColor Red
    }
} else {
    Write-Host "Pasta data limpa com sucesso" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== ETAPA 3: REMOVENDO IMAGENS ANTIGAS E RECRIANDO ===" -ForegroundColor Yellow

# Remover imagens antigas dos microserviços (exceto Redis e RabbitMQ)
Write-Host "Removendo imagens antigas dos microservicos..." -ForegroundColor Blue
$servicesToRemove = @("plataforma-auth-api", "plataforma-conteudo-api", "plataforma-alunos-api", "plataforma-pagamentos-api", "plataforma-bff-api", "plataforma-frontend")

foreach ($service in $servicesToRemove) {
    try {
        # Remove container se existir
        docker rm -f $service 2>$null | Out-Null
        
        # Remove imagem se existir
        $imageName = docker images --format "{{.Repository}}:{{.Tag}}" | Where-Object { $_ -like "*$service*" }
        if ($imageName) {
            docker rmi -f $imageName 2>$null | Out-Null
            Write-Host "  Removido: $service" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  Aviso: Nao foi possivel remover $service" -ForegroundColor Yellow
    }
}

# Remover imagens intermediárias (dangling images)
Write-Host "Removendo imagens intermediarias..." -ForegroundColor Blue
docker image prune -f | Out-Null

# Remover imagens não utilizadas (mais agressivo)
Write-Host "Limpando cache do Docker..." -ForegroundColor Blue
docker system prune -f | Out-Null

# Verificar se as imagens foram removidas
Write-Host "Verificando limpeza das imagens..." -ForegroundColor Blue
$remainingImages = docker images --format "{{.Repository}}:{{.Tag}}" | Where-Object { $_ -like "*plataforma-*" }
if ($remainingImages) {
    Write-Host "  Imagens restantes:" -ForegroundColor Yellow
    foreach ($image in $remainingImages) {
        Write-Host "    $image" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Todas as imagens antigas foram removidas" -ForegroundColor Green
}

# Limpeza adicional: remover imagens por nome específico
Write-Host "Limpando imagens por nome especifico..." -ForegroundColor Blue
$imageNamesToRemove = @(
    "mba-modulo4-auth-api",
    "mba-modulo4-conteudo-api", 
    "mba-modulo4-alunos-api",
    "mba-modulo4-pagamentos-api",
    "mba-modulo4-bff-api",
    "mba-modulo4-frontend"
)

foreach ($imageName in $imageNamesToRemove) {
    try {
        # Remove por nome da imagem
        docker rmi -f $imageName 2>$null | Out-Null
        
        # Remove por nome do repositório
        docker images --format "{{.Repository}}" | Where-Object { $_ -like "*$imageName*" } | ForEach-Object {
            docker rmi -f $_ 2>$null | Out-Null
        }
        
        Write-Host "  Limpo: $imageName" -ForegroundColor Yellow
    } catch {
        # Ignora erros de remoção
    }
}

# Recriar todas as imagens (exceto Redis e RabbitMQ)
Write-Host "Recriando imagens dos microservicos..." -ForegroundColor Blue

# Verificação final: confirmar que apenas infraestrutura permanece
Write-Host "Verificando imagens restantes..." -ForegroundColor Blue
$allImages = docker images --format "{{.Repository}}:{{.Tag}}"
$infraImages = $allImages | Where-Object { $_ -like "*redis*" -or $_ -like "*rabbitmq*" }
$appImages = $allImages | Where-Object { $_ -like "*plataforma-*" -or $_ -like "*mba-modulo4*" }

if ($appImages) {
    Write-Host "  AVISO: Ainda existem imagens de aplicacao:" -ForegroundColor Yellow
    foreach ($image in $appImages) {
        Write-Host "    $image" -ForegroundColor Yellow
    }
} else {
    Write-Host "  OK: Apenas imagens de infraestrutura permanecem" -ForegroundColor Green
}

Write-Host "  Imagens de infraestrutura mantidas:" -ForegroundColor Cyan
foreach ($image in $infraImages) {
    Write-Host "    $image" -ForegroundColor Cyan
}

$servicesToBuild = @("auth-api", "conteudo-api", "alunos-api", "pagamentos-api", "bff-api", "frontend")
docker-compose -f $composeFile build --no-cache --parallel $servicesToBuild
Write-Host "Imagens recriadas com sucesso" -ForegroundColor Green

Write-Host ""
Write-Host "=== ETAPA 4: INICIANDO INFRAESTRUTURA ===" -ForegroundColor Yellow

# 1. Redis
Write-Host "1. Iniciando Redis..." -ForegroundColor Blue
docker-compose -f $composeFile up -d redis
Start-Sleep -Seconds 5
Write-Host "   Redis iniciado" -ForegroundColor Green

# 2. RabbitMQ
Write-Host "2. Iniciando RabbitMQ..." -ForegroundColor Blue
docker-compose -f $composeFile up -d rabbitmq
Write-Host "   Aguardando RabbitMQ ficar saudavel..." -ForegroundColor Yellow

# Aguarda RabbitMQ ficar 'healthy' para evitar timeout nas APIs ao registrar filas
$rabbitContainer = "plataforma-rabbitmq"
$elapsed = 0
$timeoutSec = 90  # Otimizado: 2 minutos
$rabbitReady = $false

while ($elapsed -lt $timeoutSec -and -not $rabbitReady) {
    try {
        $status = docker inspect --format='{{.State.Health.Status}}' $rabbitContainer 2>$null
        if ($status -eq 'healthy') {
            Write-Host "   RabbitMQ HEALTHY" -ForegroundColor Green
            
            # Aguarda um pouco para garantir que o RabbitMQ esteja totalmente operacional
            Write-Host "   Aguardando RabbitMQ ficar totalmente operacional..." -ForegroundColor Yellow
            Start-Sleep -Seconds 5
            
            # Testa conectividade com o RabbitMQ
            $rabbitReady = $true
            Write-Host "   RabbitMQ pronto para receber conexoes" -ForegroundColor Green
            break
        } else {
            Write-Host "   Status RabbitMQ: $status ($elapsed/$timeoutSec)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   Aguardando RabbitMQ inicializar... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
    }
    
    Start-Sleep -Seconds 5
    $elapsed += 5
    
    if ($elapsed -ge $timeoutSec) {
        Write-Host "   AVISO: Timeout aguardando RabbitMQ HEALTHY. Prosseguindo..." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== ETAPA 5: INICIANDO MICROSERVICOS ===" -ForegroundColor Yellow

# 3. Auth API
Write-Host "3. Iniciando Auth API..." -ForegroundColor Blue
docker-compose -f $composeFile up -d auth-api
Write-Host "   Aguardando Auth API inicializar e criar banco SQLite..." -ForegroundColor Yellow

# Aguarda o banco ser criado e verifica se foi criado corretamente
Start-Sleep -Seconds 25

# Verificar se o banco SQLite foi criado corretamente
$authContainer = "plataforma-auth-api"
$authDbPath = "/app/data/auth-dev.db"
$elapsed = 0
$timeoutSec = 90
$dbReady = $false

while ($elapsed -lt $timeoutSec -and -not $dbReady) {
    try {
        $result = docker exec $authContainer sh -c "if [ -f $authDbPath ] && [ -s $authDbPath ]; then ls -la $authDbPath; else echo 'NOT_READY'; fi" 2>$null
        if ($result -and $result.Contains("NOT_READY")) {
            Write-Host "   Aguardando banco SQLite ser criado... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
        } else {
            Write-Host "   Auth API e banco SQLite prontos" -ForegroundColor Green
            $dbReady = $true
            break
        }
    } catch {
        Write-Host "   Aguardando container inicializar... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 10
    $elapsed += 10
}

if (-not $dbReady) {
    Write-Host "   AVISO: Timeout aguardando Auth API. Prosseguindo..." -ForegroundColor Yellow
}

# 4. Alunos API
Write-Host "4. Iniciando Alunos API..." -ForegroundColor Blue
docker-compose -f $composeFile up -d alunos-api
Write-Host "   Aguardando Alunos API inicializar e criar banco SQLite..." -ForegroundColor Yellow
Start-Sleep -Seconds 25

# Verificar se o banco SQLite foi criado corretamente
$alunosContainer = "plataforma-alunos-api"
$alunosDbPath = "/app/data/alunos-dev.db"
$elapsed = 0
$timeoutSec = 90
$dbReady = $false

while ($elapsed -lt $timeoutSec -and -not $dbReady) {
    try {
        $result = docker exec $alunosContainer sh -c "if [ -f $alunosDbPath ] && [ -s $alunosDbPath ]; then ls -la $alunosDbPath; else echo 'NOT_READY'; fi" 2>$null
        if ($result -and $result.Contains("NOT_READY")) {
            Write-Host "   Aguardando banco SQLite ser criado... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
        } else {
            Write-Host "   Alunos API e banco SQLite prontos" -ForegroundColor Green
            $dbReady = $true
            break
        }
    } catch {
        Write-Host "   Aguardando container inicializar... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 10
    $elapsed += 10
}

if (-not $dbReady) {
    Write-Host "   AVISO: Timeout aguardando Alunos API. Prosseguindo..." -ForegroundColor Yellow
}

# 5. Pagamentos API
Write-Host "5. Iniciando Pagamentos API..." -ForegroundColor Blue
docker-compose -f $composeFile up -d pagamentos-api
Write-Host "   Aguardando Pagamentos API inicializar e criar banco SQLite..." -ForegroundColor Yellow
Start-Sleep -Seconds 25

# Verificar se o banco SQLite foi criado corretamente
$pagamentosContainer = "plataforma-pagamentos-api"
$pagamentosDbPath = "/app/data/pagamentos-dev.db"
$elapsed = 0
$timeoutSec = 90
$dbReady = $false

while ($elapsed -lt $timeoutSec -and -not $dbReady) {
    try {
        $result = docker exec $pagamentosContainer sh -c "if [ -f $pagamentosDbPath ] && [ -s $pagamentosDbPath ]; then ls -la $pagamentosDbPath; else echo 'NOT_READY'; fi" 2>$null
        if ($result -and $result.Contains("NOT_READY")) {
            Write-Host "   Aguardando banco SQLite ser criado... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
        } else {
            Write-Host "   Pagamentos API e banco SQLite prontos" -ForegroundColor Green
            $dbReady = $true
            break
        }
    } catch {
        Write-Host "   Aguardando container inicializar... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 10
    $elapsed += 10
}

if (-not $dbReady) {
    Write-Host "   AVISO: Timeout aguardando Pagamentos API. Prosseguindo..." -ForegroundColor Yellow
}

# 6. Conteudo API
Write-Host "6. Iniciando Conteudo API..." -ForegroundColor Blue
docker-compose -f $composeFile up -d conteudo-api
Write-Host "   Aguardando Conteudo API inicializar e criar banco SQLite..." -ForegroundColor Yellow
Start-Sleep -Seconds 25

# Verificar se o banco SQLite foi criado corretamente
$conteudoContainer = "plataforma-conteudo-api"
$conteudoDbPath = "/app/data/conteudo-dev.db"
$elapsed = 0
$timeoutSec = 90
$dbReady = $false

while ($elapsed -lt $timeoutSec -and -not $dbReady) {
    try {
        $result = docker exec $conteudoContainer sh -c "if [ -f $conteudoDbPath ] && [ -s $conteudoDbPath ]; then ls -la $conteudoDbPath; else echo 'NOT_READY'; fi" 2>$null
        if ($result -and $result.Contains("NOT_READY")) {
            Write-Host "   Aguardando banco SQLite ser criado... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
        } else {
            Write-Host "   Conteudo API e banco SQLite prontos" -ForegroundColor Green
            $dbReady = $true
            break
        }
    } catch {
        Write-Host "   Aguardando container inicializar... ($elapsed/$timeoutSec)" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 10
    $elapsed += 10
}

if (-not $dbReady) {
    Write-Host "   AVISO: Timeout aguardando Conteudo API. Prosseguindo..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== ETAPA 6: INICIANDO BFF E FRONTEND ===" -ForegroundColor Yellow

# 7. BFF API
Write-Host "7. Iniciando BFF API..." -ForegroundColor Blue
docker-compose -f $composeFile up -d bff-api
Write-Host "   Aguardando BFF API inicializar..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Frontend
Write-Host "8. Iniciando Frontend..." -ForegroundColor Blue
docker-compose -f $composeFile up -d frontend
Write-Host "   Aguardando Frontend inicializar..." -ForegroundColor Yellow
Start-Sleep -Seconds 10


Write-Host ""
Write-Host "=== ETAPA 7: MONITORAMENTO ===" -ForegroundColor Yellow

Write-Host "1. Iniciando Plataforma + Monitoramento (Prometheus/Grafana)..." -ForegroundColor Blue
docker-compose -f $composeFile -f docker-compose-monitoring.yml up -d


if ($LASTEXITCODE -ne 0) {
  Write-Host "Falha ao subir os containers. Veja o log acima." -ForegroundColor Red
}


Write-Host ""
Write-Host "=== ETAPA 8: VERIFICAÇÃO FINAL ===" -ForegroundColor Yellow

# Verificação final dos bancos SQLite
Write-Host "Verificacao final dos bancos SQLite..." -ForegroundColor Blue
$allContainers = @("plataforma-auth-api", "plataforma-conteudo-api", "plataforma-alunos-api", "plataforma-pagamentos-api")
$allDbFiles = @("/app/data/auth-dev.db", "/app/data/conteudo-dev.db", "/app/data/alunos-dev.db", "/app/data/pagamentos-dev.db")

for ($i = 0; $i -lt $allContainers.Count; $i++) {
    $containerName = $allContainers[$i]
    $dbFile = $allDbFiles[$i]
    $dbName = Split-Path $dbFile -Leaf
    
    try {
        $result = docker exec $containerName sh -c "if [ -f $dbFile ] && [ -s $dbFile ]; then ls -la $dbFile; else echo 'NOT_FOUND'; fi" 2>$null
        if ($result -and $result.Contains("NOT_FOUND")) {
            Write-Host "ERRO: Banco $dbName nao foi criado em $containerName" -ForegroundColor Red
        } else {
            Write-Host "OK: Banco $dbName esta funcionando em $containerName" -ForegroundColor Green
        }
    } catch {
        Write-Host "ERRO: Nao foi possivel verificar banco $dbName em $containerName" -ForegroundColor Red
    }
}




Write-Host ""
Write-Host "Sistema iniciado com sucesso!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "URLs de Acesso:" -ForegroundColor White
Write-Host ""
Write-Host "   Frontend:          http://localhost:4200" -ForegroundColor Cyan
Write-Host "   BFF API:           http://localhost:5000" -ForegroundColor Cyan
Write-Host "   Auth API:          http://localhost:5001" -ForegroundColor Cyan
Write-Host "   Conteudo API:      http://localhost:5002" -ForegroundColor Cyan
Write-Host "   Alunos API:        http://localhost:5003" -ForegroundColor Cyan
Write-Host "   Pagamentos API:    http://localhost:5004" -ForegroundColor Cyan
Write-Host ""
Write-Host "Infraestrutura:" -ForegroundColor White
Write-Host ""
Write-Host "   RabbitMQ:          http://localhost:15672 (admin/admin123)" -ForegroundColor Magenta
Write-Host "   Redis:             http://localhost:6379" -ForegroundColor Red
Write-Host "   Prometheus:        http://localhost:9090" -ForegroundColor Green
Write-Host "   Grafana:           http://localhost:3000  (user: admin / senha: admin ou $env:GRAFANA_ADMIN_PASSWORD)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Banco de Dados (SQLite):" -ForegroundColor White
Write-Host ""
Write-Host "   Auth DB:           ./data/auth-dev.db" -ForegroundColor Green
Write-Host "   Alunos DB:         ./data/alunos-dev.db" -ForegroundColor Green
Write-Host "   Conteudo DB:       ./data/conteudo-dev.db" -ForegroundColor Green
Write-Host "   Pagamentos DB:     ./data/pagamentos-dev.db" -ForegroundColor Green
Write-Host ""
Write-Host "Status dos containers:" -ForegroundColor White
docker-compose -f $composeFile ps
Write-Host ""
Write-Host "Para ver logs: docker-compose -f $composeFile logs -f [service_name]" -ForegroundColor Yellow
Write-Host "Para parar tudo: docker-compose -f $composeFile down" -ForegroundColor Red
Write-Host ""
Write-Host "NOTA: Sistema rodando em modo DESENVOLVIMENTO com SQLite" -ForegroundColor Yellow
Write-Host ""
Write-Host "SEQUENCIA DE INICIALIZACAO EXECUTADA:" -ForegroundColor Cyan
Write-Host "1. Redis" -ForegroundColor White
Write-Host "2. RabbitMQ" -ForegroundColor White
Write-Host "3. Auth API" -ForegroundColor White
Write-Host "4. Alunos API" -ForegroundColor White
Write-Host "5. Pagamentos API" -ForegroundColor White
Write-Host "6. Conteudo API" -ForegroundColor White
Write-Host "7. BFF API" -ForegroundColor White
Write-Host "8. Frontend" -ForegroundColor White
Write-Host "9. Prometheus/Grafana" -ForegroundColor White

Write-Host "IMPORTANTE: Todos os arquivos SQLite foram removidos e recriados para evitar corrupção!" -ForegroundColor Yellow
Write-Host "Se ainda houver problemas, verifique as permissões da pasta 'data' e execute como administrador." -ForegroundColor Yellow
