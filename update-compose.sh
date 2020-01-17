$(aws ecr get-login --no-include-email --region us-east-1)

docker pull xxxxx.dkr.ecr.us-east-1.amazonaws.com/{image-name}:{image-tag}

/usr/local/bin/docker-compose up -d

# crontab entry that can kick this off
# */2 * * * * ./update-compose.sh >> /tmp/crontab.txt 2>&1