pipeline {
    agent any

    environment {
        DOCKER_COMPOSE = 'docker-compose'
        WORKSPACE_DIR = "${WORKSPACE}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Images') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose build message-sender message-receiver
                    '''
                }
            }
        }

        stage('Stop Existing Containers') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose stop rabbitmq message-sender message-receiver prometheus grafana || true
                        docker-compose rm -f rabbitmq message-sender message-receiver prometheus grafana || true
                    '''
                }
            }
        }

        stage('Start Services') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        # Start all services first
                        docker-compose up -d rabbitmq message-sender message-receiver prometheus grafana
                        
                        # Wait a moment for volumes to be created
                        sleep 3
                        
                        # Find the actual volume name (docker-compose adds project prefix)
                        ACTUAL_VOLUME=$(docker inspect prometheus 2>/dev/null | grep -A 10 '"Mounts"' | grep '"Name"' | grep prometheus-config | head -1 | sed 's/.*"Name": "\([^"]*\)".*/\1/' || echo "")
                        
                        if [ -z "$ACTUAL_VOLUME" ]; then
                            # Fallback: try to find any prometheus-config volume
                            ACTUAL_VOLUME=$(docker volume ls | grep prometheus-config | awk '{print $2}' | head -1 || echo "prometheus-config")
                        fi
                        
                        echo "Using volume: $ACTUAL_VOLUME"
                        
                        # Copy config to the volume
                        cat prometheus/prometheus.yml | docker run --rm -i \
                            -v ${ACTUAL_VOLUME}:/etc/prometheus \
                            alpine sh -c "cat > /etc/prometheus/prometheus.yml && chmod 644 /etc/prometheus/prometheus.yml && echo '--- Config copied successfully ---' && cat /etc/prometheus/prometheus.yml"
                        
                        # Restart Prometheus to reload config
                        docker-compose restart prometheus
                        
                        # Wait and verify
                        sleep 3
                        echo "--- Verifying Prometheus config ---"
                        docker exec prometheus cat /etc/prometheus/prometheus.yml | head -30
                    '''
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline completed'
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
            dir("${WORKSPACE}") {
                sh 'docker-compose logs'
            }
        }
    }
}

