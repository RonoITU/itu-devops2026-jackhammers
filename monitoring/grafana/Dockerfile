FROM grafana/grafana:12.1

ENV PROMETHEUS_HOST='prometheus:9090'

COPY dashboards /etc/grafana/provisioning/dashboards
COPY datasources /etc/grafana/provisioning/datasources

EXPOSE 3000