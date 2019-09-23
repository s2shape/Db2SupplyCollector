export DB2_HOST=localhost
export DB2_PORT=50000
export DB2_USER=db2inst1
export DB2_PASS=mydb2container123
export DB2_DB=testdb

echo "--------Creating the container--------"

docker run -d --name db2-database --privileged=true -p 50000:50000 -e LICENSE=accept -e DB2INST1_PASSWORD=$DB2_PASS -e DBNAME=$DB2_DB ibmcom/db2

echo "--------Waiting for the container initialization--------"
sleep 300

echo "--------Copying the seed data script file to the container--------"
docker cp DB2SupplyCollectorTests/tests/data.sql db2-database:/database

echo "--------Executing the seed data script in the container--------"
docker exec -i db2-database sh -c "su - db2inst1 -c 'db2 -vtf /database/data.sql'"

echo "--------Running test cases--------"
dotnet test

echo "--------Stopping the container--------"
docker stop db2-database

echo "--------Removing the container--------"
docker rm db2-database
