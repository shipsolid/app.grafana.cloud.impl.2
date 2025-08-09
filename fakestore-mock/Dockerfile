FROM node:20-alpine
WORKDIR /app
RUN npm i -g json-server@^0
COPY db.json /app/db.json
EXPOSE 3000
HEALTHCHECK --interval=10s --timeout=3s --retries=5 \
  CMD wget -qO- http://localhost:3000/products >/dev/null || exit 1
CMD [ "json-server", "--host", "0.0.0.0", "--port", "3000", "/app/db.json" ]
