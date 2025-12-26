# Plataforma (Kubernetes)

Este repositório contém **2 manifests Kubernetes** para subir a *Plataforma* no cluster:

- **`minikube-all.yaml`** → pensado para **Minikube**, usando **Ingress** (`plataforma.local`) e Services `ClusterIP`.
- **`plataforma.yaml`** → pensado para cluster “genérico” (ou Minikube), expondo **Frontend** e **BFF** via **NodePort**.

> Namespace padrão: `plataforma`

---

## 🧩 Componentes provisionados

Infra:

- **SQL Server 2022** (PVC `sqlserver-pvc`, porta 1433)
- **RabbitMQ** com Management (PVC `rabbitmq-pvc`, portas 5672 / 15672)
- **Redis 7** (PVC `redis-pvc`, porta 6379)

Aplicações:

- **auth-api** (`lelloimp/plataforma-auth-api:latest`)
- **conteudo-api** (`lelloimp/plataforma-conteudo-api:latest`)
- **alunos-api** (`lelloimp/plataforma-alunos-api:latest`)
- **pagamentos-api** (`lelloimp/plataforma-pagamentos-api:latest`)
- **bff-api** (`lelloimp/plataforma-bff-api:latest`)
- **frontend** (`lelloimp/plataforma-frontend:latest`)

---

## 🔐 ConfigMap e Secrets

### ConfigMap (`plataforma-config`)
Variáveis definidas:

- `ASPNETCORE_ENVIRONMENT`
- `JWT_ISSUER`
- `JWT_AUDIENCE`

### Secret (`plataforma-secrets`)
Valores via `stringData` (troque antes de produção):

- `SA_PASSWORD` (SQL Server)
- `RABBIT_USER`, `RABBIT_PASS`
- `JWT_SECRET`

> ⚠️ **Importante:** esses valores estão versionados no YAML para facilitar testes locais.  
> Para produção, use **External Secrets**, **Sealed Secrets** ou injete via pipeline/CI.

---

## 🚀 Deploy no Minikube (com Ingress) — `minikube-all.yaml`

### 1) Subir o Minikube
Exemplo:

```bash
minikube start --cpus=4 --memory=8192
```

### 2) Habilitar Ingress NGINX
```bash
minikube addons enable ingress
```

Aguardar os pods do ingress:

```bash
kubectl get pods -n ingress-nginx
```

### 3) Aplicar o manifest
```bash
kubectl apply -f minikube-all.yaml
kubectl get all -n plataforma
```

### 4) Mapear o host `plataforma.local`
Pegue o IP do Minikube:

```bash
minikube ip
```

Adicione no seu `hosts`:

- **Windows**: `C:\Windows\System32\drivers\etc\hosts`
- **Linux/Mac**: `/etc/hosts`

Exemplo:

```
<MINIKUBE_IP>  plataforma.local
```

### 5) Acessos (Ingress)
- **Frontend**: `http://plataforma.local/`
- **BFF**: `http://plataforma.local/api/...` (o Ingress redireciona para o service `bff-api`)

Ingress criado:

- `plataforma-ingress` (classe `nginx`)
- Regras:
  - `/` → `frontend:80`
  - `/api(/|$)(.*)` → `bff-api:5000` (com rewrite)

---

## 🚀 Deploy com NodePort — `plataforma.yaml`

Use este arquivo quando você quer acessar sem Ingress (ex.: cluster simples, laboratório, etc).

### 1) Aplicar o manifest
```bash
kubectl apply -f plataforma.yaml
kubectl get all -n plataforma
```

### 2) Descobrir os NodePorts
```bash
kubectl get svc -n plataforma
```

No `plataforma.yaml` normalmente ficam expostos via **NodePort**:

- `frontend` (porta 80)
- `bff-api` (porta 5000)

A URL final depende do seu cluster:

- `http://<NODE_IP>:<NODEPORT_DO_FRONTEND>/`
- `http://<NODE_IP>:<NODEPORT_DO_BFF>/`

#### Dica (Minikube)
No Minikube, você pode obter a URL direto:

```bash
minikube service -n plataforma frontend --url
minikube service -n plataforma bff-api --url
```

---

## 🧪 Verificação rápida

### Ver status de pods
```bash
kubectl get pods -n plataforma -o wide
```

### Ver logs de um deployment
```bash
kubectl logs -n plataforma deploy/frontend -f
kubectl logs -n plataforma deploy/bff-api -f
```

### Testar DNS interno (dentro do cluster)
```bash
kubectl run -n plataforma -it --rm dns-test --image=busybox:1.36 --restart=Never -- sh
# dentro do pod:
nslookup sqlserver
nslookup rabbitmq
nslookup redis
```

---

## 🧹 Remover tudo

```bash
kubectl delete -f minikube-all.yaml
# ou
kubectl delete -f plataforma.yaml
```

Se quiser remover também o namespace (e tudo dentro):

```bash
kubectl delete ns plataforma
```

---

## 📌 Observações

- Os PVCs não definem `storageClassName` (usam a default do cluster).
- As imagens apontam para o Docker Hub do usuário **`lelloimp`** (tag `latest`).
- Para produção, considere:
  - `resources` (requests/limits)
  - `livenessProbe`/`readinessProbe`
  - TLS no Ingress (cert-manager)
  - Secrets fora do repositório
  - Banco gerenciado (ou StatefulSet) se fizer sentido

---

### Arquivos

- `minikube-all.yaml` (Ingress / Minikube)
- `plataforma.yaml` (NodePort)
