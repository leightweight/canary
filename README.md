# Canary

A health check service for load-balanced Tailscale servers.

## Usage

The Canary server is built with .NET 8 and, optionally, Make.
On an Ubuntu server, you can ensure these are installed by running:

```shell
sudo apt install dotnet-sdk-8.0 make
```

If .NET was installed for the first time, or it was updated, update the .NET workloads:

```shell
sudo dotnet workload update
```

Then you can clone this repo:

```shell
git clone https://github.com/leightweight/canary.git
```

To build, install, and set up the Canary as a `systemd` service, run the following commands:

```shell
cd canary
make
sudo make install
sudo systemctl enable canary
sudo systemctl start canary
```

Then configure Tailscale to talk to Canary by modifying the `/etc/default/tailscaled` file to set the `--bird-socket` flag:

```shell
# /etc/default/tailscaled

FLAGS="--bird-socket=/var/run/canary.ctl"
```

Finally, restart Tailscale:

```shell
sudo systemctl restart tailscaled
```
