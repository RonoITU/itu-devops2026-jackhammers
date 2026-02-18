import argparse
import sys

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType

def main():
    parser = argparse.ArgumentParser(
        description="Python script that creates our ITU-Minitwit server on Hetzner."
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
    client = Client(
        token=args.apikey,
        application_name="make-devops-serv.py",
        application_version="v1.0.0"
    )

    volumeResponse = client.volumes.create(
        size=10, # GB
        location=client.locations.get_by_name("hel1"),
        name = args.servername + "-volume"
    )

    # Creates a server with Docker CE image
    serverResponse = client.servers.create(
        name=args.servername,
        server_type=ServerType(name="cax11"),
        image=Image(name="docker-ce"),
        location=client.locations.get_by_name("hel1"),
        volumes=[volumeResponse.volume]
    )

    server = serverResponse.server
    print("*** New server created:")
    print(f"{server.id=} {server.name=} {server.status=}")
    print(f"root password: {serverResponse.root_password}")

    # List your servers
    servers = client.servers.get_all()
    print("*** Active servers:")
    for server in servers:
        print(f"{server.id=} {server.name=} {server.status=}")

if __name__ == "__main__":
    main()
