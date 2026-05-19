import argparse
import sys

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType

# This script creates a server matching the one we currently use for 
# ITU-Minitwit. (February 18th 2026)
def main():
    parser = argparse.ArgumentParser(
        description="Python script that creates a server on Hetzner for ITU-Minitwit."
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
        application_name="make-devops-serv.py",
        application_version="v1.0.0"
    )

    APP_CLOUD_URI = "provision/autodeploy-app-cloud-init.yml"
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

    appServerResponse = client.servers.create( # Creates a server with Docker CE image
        name="ITU-Minitwit-App-Server",
        server_type=ServerType(name="cpx22"),          # Product name to provision. CPX22 is a 10€/month x86_64 server at a reliably available tier. 
        image=Image(name="docker-ce"),                 # Use the default Ubuntu image with Docker installed. 
        location=client.locations.get_by_name("hel1"), # Provision in the Helsinki data center. 
        user_data=appCloudInit
    )

    appServer = appServerResponse.server
    print("*** App server scheduled for creation:")
    print(f"{appServer.id=} {appServer.name=} {appServer.status=}")
    print(f"root password: {appServerResponse.root_password}") # Only relevant if we did not set any SSH keys. 

    monitorServerResponse = client.servers.create( # Creates a server with Docker CE image
        name="ITU-Minitwit-Monitor-Server",
        server_type=ServerType(name="cpx22"),          # Product name to provision. CPX22 is a 10€/month x86_64 server at a reliably available tier. 
        image=Image(name="docker-ce"),                 # Use the default Ubuntu image with Docker installed. 
        location=client.locations.get_by_name("hel1"), # Provision in the Helsinki data center. 
        user_data=monitorCloudInit
    )

    monitorServer = monitorServerResponse.server
    print("*** Monitor server scheduled for creation:")
    print(f"{monitorServer.id=} {monitorServer.name=} {monitorServer.status=}")
    print(f"root password: {monitorServerResponse.root_password}") # Only relevant if we did not set any SSH keys. 

    # List your servers
    servers = client.servers.get_all()
    print("*** All current or pending servers:")
    for s in servers:
        print(f"{s.id=} {s.name=} {s.status=}")

if __name__ == "__main__":
    main()
