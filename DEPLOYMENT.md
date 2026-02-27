# Deployment Guide — kanellos.me

**Domains:**
- `api.kanellos.me` → Backend API (this app)
- `kanellos.me` → Frontend (separate project)

## GitHub Repository Secrets

Add these in **Settings → Secrets and variables → Actions**:

| Secret | Value (example) |
|--------|-----------------|
| `DEPLOY_SSH_KEY_B64` | Base64-encoded SSH private key |
| `DEPLOY_HOST` | `api.kanellos.me` (or your server IP) |
| `DEPLOY_USER` | `kanellos` |
| `DEPLOY_PATH` | `/srv/portfolio-backend` |

### Creating the deploy key (Base64)

On your **local machine** (macOS or Linux):

```bash
# Generate key pair (no passphrase)
ssh-keygen -t ed25519 -C "github-deploy" -f deploy_key -N ""

# Base64 private key (for DEPLOY_SSH_KEY_B64) — macOS:
base64 -i deploy_key | tr -d '\n'

# Base64 private key — Linux:
base64 -w 0 deploy_key

# Public key (add to server ~/.ssh/authorized_keys)
cat deploy_key.pub
```

---

## Server Setup

### 1. Add the deploy public key

If using user `kanellos`:

```bash
mkdir -p ~/.ssh
echo "ssh-ed25519 AAAA... github-deploy" >> ~/.ssh/authorized_keys
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

### 2. Install .NET 9 runtime

```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
sudo ./dotnet-install.sh --channel 9.0 --runtime aspnetcore
# Or use Microsoft package feed: https://learn.microsoft.com/en-us/dotnet/core/install/linux
```

### 3. Create app directory

```bash
sudo mkdir -p /srv/portfolio-backend
sudo chown kanellos:kanellos /srv/portfolio-backend
```

### 4. Create `.env` on the server

The app reads configuration from `.env`. Create it in the app directory:

```bash
nano /srv/portfolio-backend/.env
```

Required variables (production values):

```
Jwt__Key=<your-secure-jwt-key>
Jwt__Issuer=https://api.kanellos.me
Jwt__Audience=https://api.kanellos.me
ConnectionStrings__DefaultConnection=<your-postgres-connection-string>
Cloudflare__ZoneId=<your-zone-id>
Cloudflare__ApiToken=<your-token>
FRONTEND_URL=https://kanellos.me
Smtp__Host=<smtp-host>
Smtp__Port=465
Smtp__Username=<email>
Smtp__Password=<password>
Smtp__FromEmail=<email>
Smtp__FromName=<name>
```

### 5. Create wwwroot/uploads

```bash
mkdir -p /srv/portfolio-backend/wwwroot/uploads
```

### 6. systemd service

Create `/etc/systemd/system/portfolio-backend.service`:

```ini
[Unit]
Description=Portfolio Backend API
After=network.target

[Service]
WorkingDirectory=/srv/portfolio-backend
ExecStart=/usr/bin/dotnet portfolio-backend.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=ASPNETCORE_ENVIRONMENT=Production

User=kanellos
Group=kanellos

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable portfolio-backend
sudo systemctl start portfolio-backend
```

### 7. Allow passwordless service restart (for GitHub Actions)

```bash
sudo visudo
# Add:
kanellos ALL=(ALL) NOPASSWD: /bin/systemctl restart portfolio-backend
```

### 8. Nginx + SSL (api.kanellos.me)

Ensure `api.kanellos.me` DNS points to your server, then:

```bash
# Install nginx and certbot
sudo apt update
sudo apt install nginx certbot python3-certbot-nginx -y

# Create nginx config
sudo nano /etc/nginx/sites-available/portfolio-api
```

Paste:

```nginx
server {
    listen 80;
    server_name api.kanellos.me;
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/portfolio-api /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# Get SSL certificate
sudo certbot --nginx -d api.kanellos.me
```

---

## Workflow behavior

- Triggers on **push to `main`**
- Builds and publishes the .NET app
- Rsyncs the publish output to `DEPLOY_PATH`
- Restarts the `portfolio-backend` systemd service

The first deployment requires the server to be set up and the service created. After that, each push to `main` deploys automatically.
