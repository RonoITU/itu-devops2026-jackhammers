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
        "servername",
        type=str,
        help="Server name"
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

    volumeResponse = client.volumes.create( # Provision a 10 GB volume for the server database. 
        size=10,
        name = args.servername + "-volume",
        location=client.locations.get_by_name("hel1")  # Provision in the Helsinki data center.
    )

    serverResponse = client.servers.create( # Creates a server with Docker CE image
        name=args.servername,
        server_type=ServerType(name="cax11"),          # Provision the small CAX11 ARM64 server.
        image=Image(name="docker-ce"),                 # Use the default Ubuntu image with Docker installed. 
        location=client.locations.get_by_name("hel1"), # Provision in the Helsinki data center. 
        volumes=[volumeResponse.volume],               # This will mount the volume we just created.
        ssh_keys=client.ssh_keys.get_all()             # ALL public SSH keys in the project will be authorized for root logon!
    )

    server = serverResponse.server
    print("*** New server being created:")
    print(f"{server.id=} {server.name=} {server.status=}")
    print(f"root password: {serverResponse.root_password}") # Only relevant if we did not set any SSH keys. 

    # List your servers
    servers = client.servers.get_all()
    print("*** Active servers:")
    for server in servers:
        print(f"{server.id=} {server.name=} {server.status=}")

if __name__ == "__main__":
    main()
