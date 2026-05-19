import argparse
import sys

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType
from hcloud.firewalls.domain import FirewallRule

# This script creates a server matching the one we currently use for 
# ITU-Minitwit. (February 18th 2026)
def main():
    parser = argparse.ArgumentParser(
        description="Python script that configures infrastructure for ITU-Minitwit on Hetzner Cloud."
    )

    parser.add_argument(
        "-k", "--apikey",
        type=str,
        required=True,
        help="Hetzner Project API key"
    )

    args = parser.parse_args()

    # Closely follows the example found at:
    # https://github.com/hetznercloud/hcloud-python
    # See Python API docs at:
    # https://hcloud-python.readthedocs.io/en/stable/index.html

    client = Client(
        token=args.apikey,
        application_name="Configure infrastructure for ITU-Minitwit on Hetzner Cloud",
        application_version="v1.0.0"
    )

    if client.firewalls.get_by_name("https-app-firewall"):
        print("App firewall already exists. Skipping creation.")
    else:
        client.firewalls.create(
            name="https-app-firewall",
            rules=[
                FirewallRule(description="Allow SSH", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="22"),
                FirewallRule(description="Allow HTTP via TCP", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="80"),
                FirewallRule(description="Allow HTTPS via TCP", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="443"),
                FirewallRule(description="Allow HTTPS via UDP (QUIC Protocol)", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="udp", port="443")
            ]
        )

    if client.firewalls.get_by_name("https-monitor-firewall"):
        print("Monitor firewall already exists. Skipping creation.")
    else:
        client.firewalls.create(
            name="https-monitor-firewall",
            rules=[
                FirewallRule(description="Allow SSH", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="22"),
                FirewallRule(description="Grafana", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="3000"),
                FirewallRule(description="Prometheus", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="tcp", port="9090"),
                FirewallRule(description="Loki", direction="in", source_ips=["0.0.0.0/0", "::/0"], protocol="udp", port="3100")
            ]
        )

    # List your firewalls
    firewalls = client.firewalls.get_all()
    print("* All current firewalls:")
    for f in firewalls:
        print(f"  {f.name}")

    APP_SERVER_NAME = "ITU-Minitwit-App-Server"
    APP_CLOUD_URI = "provision/autodeploy-app-cloud-init.yml"
    MONITOR_SERVER_NAME = "ITU-Minitwit-Monitor-Server"
    MONITOR_CLOUD_URI = "provision/autodeploy-monitoring-cloud-init.yml"

    appCloudInit = None
    with open(APP_CLOUD_URI) as f:
        appCloudInit = f.read()

    if appCloudInit is None or appCloudInit == "":
        print(f"Failed to read {APP_CLOUD_URI} file.")
        sys.exit(1)

    monitorCloudInit = None
    with open(MONITOR_CLOUD_URI) as f:
        monitorCloudInit = f.read()

    if monitorCloudInit is None or monitorCloudInit == "":
        print(f"Failed to read {MONITOR_CLOUD_URI} file.")
        sys.exit(1)

    existing_app = client.servers.get_by_name(APP_SERVER_NAME)
    if existing_app:
        print("App server already exists, skipping creation.")
    else:
        appServerResponse = client.servers.create( # Creates a server with Docker CE image
            name=APP_SERVER_NAME,
            server_type=ServerType(name="cpx22"),          # Product name to provision. CPX22 is a 10€/month x86_64 server at a reliably available tier. 
            image=Image(name="docker-ce"),                 # Use the default Ubuntu image with Docker installed. 
            location=client.locations.get_by_name("hel1"), # Provision in the Helsinki data center. 
            user_data=appCloudInit,
            firewalls=[client.firewalls.get_by_name("https-app-firewall")]
        )
        appServer = appServerResponse.server
        print("*** App server scheduled for creation:")
        print(f"{appServer.id=} {appServer.name=} {appServer.status=}")
        print(f"root password: {appServerResponse.root_password}") # Only relevant if we did not set any SSH keys. 

    existing_monitor = client.servers.get_by_name(MONITOR_SERVER_NAME)
    if existing_monitor:
        print("Monitor server already exists, skipping creation.")
    else:
        monitorServerResponse = client.servers.create( # Creates a server with Docker CE image
            name=MONITOR_SERVER_NAME,
            server_type=ServerType(name="cpx22"),          # Product name to provision. CPX22 is a 10€/month x86_64 server at a reliably available tier. 
            image=Image(name="docker-ce"),                 # Use the default Ubuntu image with Docker installed. 
            location=client.locations.get_by_name("hel1"), # Provision in the Helsinki data center. 
            user_data=monitorCloudInit,
            firewalls=[client.firewalls.get_by_name("https-monitor-firewall")]
        )
        monitorServer = monitorServerResponse.server
        print("*** Monitor server scheduled for creation:")
        print(f"{monitorServer.id=} {monitorServer.name=} {monitorServer.status=}")
        print(f"root password: {monitorServerResponse.root_password}") # Only relevant if we did not set any SSH keys. 

    # List your servers
    servers = client.servers.get_all()
    print("* All current or pending servers:")
    for s in servers:
        print(f"  {s.id=} {s.name=} {s.status=}")

if __name__ == "__main__":
    main()
