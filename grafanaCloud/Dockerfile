FROM grafana/alloy:latest

RUN apt update && apt install -y curl net-tools

COPY config.alloy /etc/alloy/config.alloy

CMD [ "run", "/etc/alloy/config.alloy", "--server.http.listen-addr=0.0.0.0:12345", "--storage.path=/var/lib/alloy/data" ]