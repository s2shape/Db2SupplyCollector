FROM ibmcom/db2

MAINTAINER burilovmv@gmail.com

ADD data.sql docker-entrypoint-initdb.d/

COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN ls -la *.sh; chmod +x *.sh; ls -la *.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["/var/db2_setup/lib/setup_db2_instance.sh"]
