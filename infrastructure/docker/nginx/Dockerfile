FROM nginx:alpine

# Generate self-signed certificate for HTTPS
RUN apk add --no-cache openssl && \
    mkdir -p /etc/nginx/ssl && \
    openssl req -x509 -nodes -days 365 \
      -newkey rsa:2048 \
      -keyout /etc/nginx/ssl/server.key \
      -out /etc/nginx/ssl/server.crt \
      -subj "/CN=localhost/O=EnterpriseRAG/C=US" && \
    apk del openssl

COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 443
