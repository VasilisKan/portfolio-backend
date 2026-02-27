# Add api.kanellos.me to Traefik

Your backend runs as **systemd** on the host (`localhost:5000`). Traefik runs in Docker and needs to reach it via `host.docker.internal`.

## 1. Create Traefik dynamic config for the API

On your server, in your **portfolio frontend** project directory:

```bash
mkdir -p traefik/dynamic
nano traefik/dynamic/api.yml
```

Paste:

```yaml
http:
  routers:
    api:
      rule: "Host(`api.kanellos.me`)"
      service: api-backend
      entrypoints:
        - web
      # Uncomment after adding HTTPS (step 3):
      # entrypoints:
      #   - websecure
      # certificateresolver: letsencrypt
  services:
    api-backend:
      loadBalancer:
        servers:
          - url: "http://host.docker.internal:5000"
        passHostHeader: true
```

## 2. Update your docker-compose.yml (Traefik service)

Add the file provider, host gateway, and the dynamic config volume:

```yaml
services:
  traefik:
    image: traefik:v2.10
    container_name: traefik
    restart: always
    command:
      - --api.dashboard=true
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --providers.file.directory=/etc/traefik/dynamic
      - --providers.file.watch=true
      - --entrypoints.web.address=:80
    ports:
      - "80:80"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./traefik/dynamic:/etc/traefik/dynamic:ro
    extra_hosts:
      - "host.docker.internal:host-gateway"
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.traefik-dashboard.rule=Host(`analytics.kanellos.me`)"
      - "traefik.http.routers.traefik-dashboard.service=api@internal"
      - "traefik.http.routers.traefik-dashboard.entrypoints=web"
      - "traefik.http.routers.traefik-dashboard.middlewares=dashboard-auth"
      - "traefik.http.middlewares.dashboard-auth.basicauth.users=analytics:$$2y$$05$$C5o53S5OTcOJHlrxvI7JiewTLkXXqLyBnovOvWsnglcy3ph2Zp53q"

  vue-app:
    # ... keep as is ...
```

## 3. Add HTTPS (optional but recommended)

Update the Traefik `command` section to add port 443 and Let's Encrypt:

```yaml
    command:
      - --api.dashboard=true
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --providers.file.directory=/etc/traefik/dynamic
      - --providers.file.watch=true
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.letsencrypt.acme.httpchallenge=true
      - --certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
      - --certificatesresolvers.letsencrypt.acme.email=YOUR_EMAIL@example.com
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./traefik/dynamic:/etc/traefik/dynamic:ro
      - letsencrypt:/letsencrypt
```

Add the volume at the bottom:

```yaml
volumes:
  letsencrypt:
```

Then update `traefik/dynamic/api.yml` to use HTTPS:

```yaml
http:
  routers:
    api:
      rule: "Host(`api.kanellos.me`)"
      service: api-backend
      entrypoints:
        - websecure
      certificateresolver: letsencrypt
    api-http:
      rule: "Host(`api.kanellos.me`)"
      service: api-backend
      entrypoints:
        - web
      middlewares:
        - redirect-to-https
  middlewares:
    redirect-to-https:
      redirectscheme:
        scheme: https
        permanent: true
  services:
    api-backend:
      loadBalancer:
        servers:
          - url: "http://host.docker.internal:5000"
        passHostHeader: true
```

## 4. Restart

```bash
cd /path/to/portfolio-frontend
docker compose up -d
```

## 5. Test

```bash
curl -I http://api.kanellos.me/api/auth/login
# With HTTPS:
curl -I https://api.kanellos.me/api/auth/login
```

---

**Note:** `host.docker.internal` with `host-gateway` works on Docker 20.10+. If you're on an older version and it fails, use your server's internal IP (e.g. `172.17.0.1` or the result of `hostname -I | awk '{print $1}'`).
