echo "--------Creating the container--------"
docker run -itd --name db2-database --privileged=true -p 50000:50000 -e LICENSE=accept -e DB2INST1_PASSWORD=mydb2container123 -e DBNAME=testdb ibmcom/db2

echo "--------Waiting for the container initialization--------"
sleep 300

echo "--------Copying the seed data script file to the container--------"
docker cp test/data.sql db2-database:/database

echo "--------Executing the seed data script in the container--------"
docker exec -i db2-database sh -c "su - db2inst1 -c 'db2 -vtf /DB2SupplyCollectorTests/tests/data.sql'"

echo "--------Running test cases--------"
dotnet test

echo "--------Stopping the container--------"
docker stop db2-database

echo "--------Removing the container--------"
docker rm db2-database
