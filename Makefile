.PHONY: default
default: build

artifacts/publish/Canary/release/Canary: $(shell find src -type f)
	dotnet publish -c Release

.PHONY: build
build: artifacts/publish/Canary/release/Canary

/etc/systemd/system/canary.service: contrib/systemd/canary.service
	cp contrib/systemd/canary.service /etc/systemd/system/canary.service

/usr/sbin/canary: artifacts/publish/Canary/release/Canary
	cp artifacts/publish/Canary/release/Canary /usr/sbin/canary

.PHONY: install
install: /etc/systemd/system/canary.service /usr/sbin/canary

.PHONY: clean
clean:
	rm -rf artifacts
