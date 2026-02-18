import argparse

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType

def main():
    parser = argparse.ArgumentParser(
        description="Python script that lists locations."
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
        application_name="get-locations.py",
        application_version="v1.0.0"
    )

    locationResponse = client.locations.get_all()
    print("*** Locations:")
    for l in locationResponse:
        print(l.name)

    # List your servers
    servers = client.servers.get_all()
    print("*** Active servers:")
    for server in servers:
        print(f"{server.id=} {server.name=} {server.status=}")

if __name__ == "__main__":
    main()
